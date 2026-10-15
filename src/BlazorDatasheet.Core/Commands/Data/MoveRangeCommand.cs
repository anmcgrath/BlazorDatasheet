using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core.Dependencies;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Moves a block of cells, as when they are cut and pasted. The cells (values, formulas, formats, merges and
/// types) land with the source's top-left on the target's top-left and the source is cleared. Formulas inside the
/// block keep pointing at the same cells (references into the block move with it), and formulas anywhere in the
/// workbook that reference cells fully inside the block are rewritten to the new location. References that only
/// partly overlap the block are left alone, as in Excel. The same goes for named ranges and the formulas of
/// conditional formats on every sheet.
/// <para>
/// When both values and formats are moved (as by default), the validators, cell metadata and conditional formats
/// of the block move with it too, replacing those at the target. Conditional formats only move within a sheet: on
/// a move to another sheet they stay where they are. <see cref="CellStore.MetaDataChanged"/> is fired for each
/// metadata item that leaves the source or changes at the target, and again when the move is undone.
/// </para>
/// <para>The command is executed on the target sheet; the source may be another sheet of the same workbook.</para>
/// </summary>
public class MoveRangeCommand : BaseCommand, IUndoableCommand
{
    private readonly SheetRange _from;
    private readonly SheetRange _to;
    private readonly CopyOptions _options;

    private CellStoreRestoreData? _copyRestore;
    private readonly List<(Sheet sheet, CellStoreRestoreData restore)> _dependentRestores = new();
    private CellStoreRestoreData? _sourceRestore;

    // Undoes the moves of validators, metadata and conditional formats and the rewrites of named ranges and
    // conditional format formulas, step by step in reverse.
    private readonly List<Action> _regionDataUndo = new();

    /// <param name="from">The cells to move. Only the single region of the range is used.</param>
    /// <param name="to">Where the top-left of the moved block lands. Only the top-left of the range is used.</param>
    /// <param name="options">What to move. By default everything is moved.</param>
    public MoveRangeCommand(SheetRange from, SheetRange to, CopyOptions? options = null)
    {
        _from = from;
        _to = to;
        // a copy, not the caller's instance: it may be reused for an ordinary copy
        _options = options?.Clone() ?? new CopyOptions();
        _options.FormulaReferences = ReferenceAdjustment.MoveWithSource;
    }

    /// <summary>
    /// The source region clipped to its sheet, or null if there is nothing to move.
    /// </summary>
    private IRegion? SourceRegion => _from.Region.GetIntersection(_from.Sheet.Region);

    /// <summary>
    /// The region the block lands in, clipped to the target sheet.
    /// </summary>
    private IRegion? GetTargetRegion(Sheet target)
    {
        var source = SourceRegion;
        if (source == null)
            return null;
        var top = _to.Region.Top;
        var left = _to.Region.Left;
        return new Region(top, top + source.Height - 1, left, left + source.Width - 1).GetIntersection(target.Region);
    }

    /// <summary>
    /// Whether the cells move with everything attached to them: validators, metadata and conditional formats.
    /// </summary>
    private bool MovesAttachments => _options.CopyValues && _options.CopyFormat;

    private bool IsNoOp(Sheet target) =>
        ReferenceEquals(_from.Sheet, target) && SourceRegion?.Equals(GetTargetRegion(target)) == true;

    public override bool CanExecuteProtected(Sheet target)
    {
        var source = SourceRegion;
        var toRegion = GetTargetRegion(target);
        if (source == null || toRegion == null)
            return true;

        var editsCells = _options.CopyValues || _options.CopyFormula;
        var formats = _options.CopyFormat || _options.CopyMerges;
        return (!editsCells || (_from.Sheet.Protection.CanEdit(source) && target.Protection.CanEdit(toRegion))) &&
               (!formats || (_from.Sheet.Protection.CanFormat(source) && target.Protection.CanFormat(toRegion)));
    }

    protected override bool CanExecuteCore(Sheet target)
    {
        var source = SourceRegion;
        var toRegion = GetTargetRegion(target);
        if (source == null || toRegion == null || IsNoOp(target))
            return false;
        if (_from.Sheet.Cells.ContainsReadOnly(source) || target.Cells.ContainsReadOnly(toRegion))
            return false;

        // The block can't land across the edge of a merged cell, as in Excel. A merge being moved may overlap
        // its own destination, though: it is taken off the source before the block lands.
        var sameSheet = ReferenceEquals(_from.Sheet, target);
        return target.Cells.GetMerges(toRegion).All(m => toRegion.Contains(m) || (sameSheet && source.Contains(m)));
    }

    protected override bool ExecuteCore(Sheet target)
    {
        var source = SourceRegion;
        var toRegion = GetTargetRegion(target);
        if (source == null || toRegion == null || IsNoOp(target))
            return false;

        _dependentRestores.Clear();
        _regionDataUndo.Clear();
        var fromSheet = _from.Sheet;
        var sameSheet = ReferenceEquals(fromSheet, target);
        var dr = toRegion.Top - source.Top;
        var dc = toRegion.Left - source.Left;

        using var updates = target.SuspendUpdates();
        if (!sameSheet)
            fromSheet.BatchUpdates();

        // Formulas elsewhere that read the moved cells, found before anything changes. Formulas inside the
        // block move with it, and anything the block lands on is overwritten, so neither is rewritten.
        var dependents = new List<(string sheetName, int row, int col)>();
        var namedDependents = new List<string>();
        if (_options.CopyFormula)
        {
            foreach (var vertex in target.FormulaEngine.DependencyManager.GetDirectDependents(source, fromSheet.Name))
            {
                if (vertex.VertexType == VertexType.Named && vertex.Formula != null)
                    namedDependents.Add(vertex.Key.Name);
                if (vertex.VertexType != VertexType.Cell || vertex.Formula == null)
                    continue;
                var inSource = vertex.SheetName == fromSheet.Name && source.Contains(vertex.Row, vertex.Col);
                var inTarget = vertex.SheetName == target.Name && toRegion.Contains(vertex.Row, vertex.Col);
                if (!inSource && !inTarget)
                    dependents.Add((vertex.SheetName, vertex.Row, vertex.Col));
            }
        }

        // Whole rows (columns) moved along their axis carry whole-row (column) references with them too.
        var referenceRegion = (_from.Region is RowRegion && dc == 0) || (_from.Region is ColumnRegion && dr == 0)
            ? _from.Region
            : source;

        _copyRestore = target.Cells.CopyImpl(_from, toRegion, _options);

        foreach (var (sheetName, row, col) in dependents.Distinct())
        {
            var sheet = target.Workbook.GetSheet(sheetName);
            var existing = sheet?.Cells.GetFormula(row, col);
            if (sheet == null || existing == null)
                continue;

            var rewritten = target.FormulaEngine.CloneFormula(existing, sheet.Name);
            if (!rewritten.MoveReferencesWithin(referenceRegion, fromSheet.Name, dr, dc, target.Name, sheet.Name))
                continue;
            _dependentRestores.Add((sheet, sheet.Cells.SetFormulaImpl(row, col, rewritten)));
        }

        _sourceRestore = new CellStoreRestoreData();
        var leftover = sameSheet ? source.Break(toRegion) : new List<IRegion> { source };
        foreach (var region in leftover)
        {
            if (_options.CopyValues || _options.CopyFormula)
                _sourceRestore.Merge(fromSheet.Cells.ClearCellsImpl(new[] { region }));
            if (_options.CopyFormat)
                _sourceRestore.Merge(fromSheet.Cells.CutFormatImpl(region));
            if (_options.CopyMerges)
                _sourceRestore.Merge(fromSheet.Cells.UnMergeImpl(region));
            if (_options.CopyTypes)
                _sourceRestore.Merge(fromSheet.Cells.SetCellTypeImpl(region, null));
            fromSheet.MarkDirty(region);
        }

        if (MovesAttachments)
        {
            var emitsMetaData = HasMetaDataListeners(fromSheet, target);
            var metaData = emitsMetaData ? SnapshotMetaData(fromSheet, target, source, toRegion) : default;
            MoveAttachments(fromSheet, target, source, toRegion);
            if (emitsMetaData)
                EmitMetaDataChanges(fromSheet, target, source, toRegion, metaData);
        }

        if (_options.CopyFormula)
        {
            MoveNamedRanges(target.FormulaEngine, namedDependents.Distinct(), referenceRegion, fromSheet.Name, dr,
                dc, target.Name);
            foreach (var sheet in target.Workbook.Sheets)
            {
                var cf = sheet.ConditionalFormats;
                if (cf.MoveReferencesWithin(referenceRegion, fromSheet.Name, dr, dc, target.Name) is { } restore)
                    _regionDataUndo.Add(() => cf.Restore(restore));
            }
        }

        if (!sameSheet)
            fromSheet.EndBatchUpdates();

        return true;
    }

    private void MoveAttachments(Sheet fromSheet, Sheet target, IRegion source, IRegion toRegion)
    {
        var fromValidators = fromSheet.Validators;
        var toValidators = target.Validators;
        MoveRegionData(fromValidators.Store, toValidators.Store, source, toRegion,
            index => fromValidators.GetValidator(index) is { } validator ? toValidators.Register(validator) : index);

        MoveRegionData(fromSheet.Cells.GetMetaDataStore(), target.Cells.GetMetaDataStore(), source, toRegion,
            metadata => metadata);

        if (ReferenceEquals(fromSheet, target))
        {
            var cf = target.ConditionalFormats;
            foreach (var step in cf.MoveImpl(source, toRegion))
                _regionDataUndo.Add(() => cf.Restore(step));
        }
    }

    private static bool HasMetaDataListeners(Sheet fromSheet, Sheet target) =>
        fromSheet.Cells.HasMetaDataListeners || target.Cells.HasMetaDataListeners;

    private static (Dictionary<(int row, int col, string name), object> from,
        Dictionary<(int row, int col, string name), object> to) SnapshotMetaData(Sheet fromSheet, Sheet target,
            IRegion source, IRegion toRegion) =>
        ReferenceEquals(fromSheet, target)
            ? (target.Cells.GetMetaDataItems([source, toRegion]), [])
            : (fromSheet.Cells.GetMetaDataItems([source]), target.Cells.GetMetaDataItems([toRegion]));

    /// <summary>
    /// Fires the metadata events for what changed in the source and target since <paramref name="before"/> was
    /// taken with <see cref="SnapshotMetaData"/>.
    /// </summary>
    private static void EmitMetaDataChanges(Sheet fromSheet, Sheet target, IRegion source, IRegion toRegion,
        (Dictionary<(int row, int col, string name), object> from,
            Dictionary<(int row, int col, string name), object> to) before)
    {
        var after = SnapshotMetaData(fromSheet, target, source, toRegion);
        fromSheet.Cells.EmitMetaDataChanges(before.from, after.from);
        if (!ReferenceEquals(fromSheet, target))
            target.Cells.EmitMetaDataChanges(before.to, after.to);
    }

    /// <summary>
    /// Rewrites the named ranges (and other named formulas) reading the moved cells to read where they land.
    /// </summary>
    private void MoveNamedRanges(FormulaEngine.FormulaEngine engine, IEnumerable<string> names, IRegion region,
        string fromSheetName, int dr, int dc, string targetSheetName)
    {
        foreach (var name in names)
        {
            if (engine.DependencyManager.GetFormula(name) is not { } existing)
                continue;

            var oldText = existing.ToFormulaString();
            // Named formulas are not on a sheet, so their references always name their sheet.
            var rewritten = engine.CloneFormula(existing, "");
            if (!rewritten.MoveReferencesWithin(region, fromSheetName, dr, dc, targetSheetName, ""))
                continue;

            engine.SetVariable(name, rewritten.ToFormulaString());
            _regionDataUndo.Add(() => engine.SetVariable(name, oldText));
        }
    }

    /// <summary>
    /// Moves the data of a region store in <paramref name="from"/> to the block at <paramref name="to"/>, which
    /// is cleared first, leaving <paramref name="from"/> empty. <paramref name="map"/> gives the data to store
    /// at the target for the data at the source.
    /// </summary>
    private void MoveRegionData<T>(RegionDataStore<T> fromStore, RegionDataStore<T> toStore, IRegion from,
        IRegion to, Func<T, T> map) where T : IEquatable<T>
    {
        var dr = to.Top - from.Top;
        var dc = to.Left - from.Left;
        var pieces = fromStore.GetDataRegions(from)
            .Select(x => (Data: map(x.Data), Region: x.Region.GetIntersection(from)!))
            .ToList();

        var clearFrom = fromStore.Clear(from);
        _regionDataUndo.Add(() => fromStore.Restore(clearFrom));
        var clearTo = toStore.Clear(to);
        _regionDataUndo.Add(() => toStore.Restore(clearTo));

        foreach (var (data, region) in pieces)
        {
            region.Shift(dr, dc);
            var clipped = region.GetIntersection(to);
            if (clipped == null)
                continue;
            var added = toStore.Add(clipped, data);
            _regionDataUndo.Add(() => toStore.Restore(added));
        }
    }

    public bool Undo(Sheet target)
    {
        var toRegion = GetTargetRegion(target);
        var fromSheet = _from.Sheet;
        var sameSheet = ReferenceEquals(fromSheet, target);

        using var updates = target.SuspendUpdates();
        if (!sameSheet)
            fromSheet.BatchUpdates();

        var source = SourceRegion;
        var emitsMetaData = MovesAttachments && _regionDataUndo.Count > 0 && source != null && toRegion != null &&
                            HasMetaDataListeners(fromSheet, target);
        var metaData = emitsMetaData ? SnapshotMetaData(fromSheet, target, source!, toRegion!) : default;

        for (var i = _regionDataUndo.Count - 1; i >= 0; i--)
            _regionDataUndo[i]();
        _regionDataUndo.Clear();

        if (emitsMetaData)
            EmitMetaDataChanges(fromSheet, target, source!, toRegion!, metaData);

        if (_sourceRestore != null)
            fromSheet.Cells.Restore(_sourceRestore);

        for (var i = _dependentRestores.Count - 1; i >= 0; i--)
            _dependentRestores[i].sheet.Cells.Restore(_dependentRestores[i].restore);

        if (_copyRestore != null && toRegion != null)
        {
            if (_options.CopyValues || _options.CopyFormula)
                target.Cells.ClearCellsImpl(new[] { toRegion });
            target.Cells.Restore(_copyRestore);
        }

        if (!sameSheet)
            fromSheet.EndBatchUpdates();

        return true;
    }

    public int RetainedCellCount =>
        (_copyRestore?.RetainedCellCount ?? 0) +
        (_sourceRestore?.RetainedCellCount ?? 0) +
        _dependentRestores.Sum(x => x.restore.RetainedCellCount);
}
