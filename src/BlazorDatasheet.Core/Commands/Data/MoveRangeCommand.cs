using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Moves a block of cells, as when they are cut and pasted. The cells (values, formulas, formats, merges and
/// types) land with the source's top-left on the target's top-left and the source is cleared. Formulas inside the
/// block keep pointing at the same cells (references into the block move with it), and formulas anywhere in the
/// workbook that reference cells fully inside the block are rewritten to the new location. References that only
/// partly overlap the block are left alone, as in Excel.
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
        return !_from.Sheet.Cells.ContainsReadOnly(source) && !target.Cells.ContainsReadOnly(toRegion);
    }

    protected override bool ExecuteCore(Sheet target)
    {
        var source = SourceRegion;
        var toRegion = GetTargetRegion(target);
        if (source == null || toRegion == null || IsNoOp(target))
            return false;

        _dependentRestores.Clear();
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
        if (_options.CopyFormula)
        {
            foreach (var vertex in target.FormulaEngine.DependencyManager.GetDirectDependents(source, fromSheet.Name))
            {
                if (vertex.VertexType != VertexType.Cell || vertex.Formula == null)
                    continue;
                var inSource = vertex.SheetName == fromSheet.Name && source.Contains(vertex.Row, vertex.Col);
                var inTarget = vertex.SheetName == target.Name && toRegion.Contains(vertex.Row, vertex.Col);
                if (!inSource && !inTarget)
                    dependents.Add((vertex.SheetName, vertex.Row, vertex.Col));
            }
        }

        _copyRestore = target.Cells.CopyImpl(_from, toRegion, _options);

        foreach (var (sheetName, row, col) in dependents.Distinct())
        {
            var sheet = target.Workbook.GetSheet(sheetName);
            var existing = sheet?.Cells.GetFormula(row, col);
            if (sheet == null || existing == null)
                continue;

            var rewritten = target.FormulaEngine.CloneFormula(existing, sheet.Name);
            if (!rewritten.MoveReferencesWithin(source, fromSheet.Name, dr, dc, target.Name, sheet.Name))
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

        if (!sameSheet)
            fromSheet.EndBatchUpdates();

        return true;
    }

    public bool Undo(Sheet target)
    {
        var toRegion = GetTargetRegion(target);
        var fromSheet = _from.Sheet;
        var sameSheet = ReferenceEquals(fromSheet, target);

        using var updates = target.SuspendUpdates();
        if (!sameSheet)
            fromSheet.BatchUpdates();

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
