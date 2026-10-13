using System.Runtime.CompilerServices;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]
[assembly: InternalsVisibleTo("BlazorDatasheet")]

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    public Sheet Sheet { get; }

    private readonly CellValue _defaultCellValue = new(null);

    public CellStore(Sheet sheet)
    {
        Sheet = sheet;
        _dataStore = new SparseMatrixStoreByRows<CellValue>(_defaultCellValue);
    }

    internal CellStore(Sheet sheet, CellValue[][] values)
    {
        Sheet = sheet;
        var dataStore = new SparseMatrixStoreByRows<CellValue>(_defaultCellValue);
        dataStore.BulkLoad(values);
        _dataStore = dataStore;
    }

    /// <summary>
    /// Returns all cells in the specified region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegion(IRegion region)
    {
        return (new SheetRange(Sheet, region))
            .Positions
            .Select(x => this.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Returns all cells that are present in the regions given.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegions(IEnumerable<IRegion> regions)
    {
        var cells = new List<IReadOnlyCell>();
        foreach (var region in regions)
            cells.AddRange(GetCellsInRegion(region));
        return cells;
    }

    /// <summary>
    /// Returns the cell at the specified position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(int row, int col)
    {
        return new SheetCell(row, col, Sheet);
    }

    /// <summary>
    /// Returns the cell at the specified position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(CellPosition position)
    {
        return GetCell(position.row, position.col);
    }

    internal IEnumerable<CellPosition> GetNonEmptyCellPositions(IRegion region)
    {
        return _dataStore.GetNonEmptyPositions(region.TopLeft.row,
            region.BottomRight.row,
            region.TopLeft.col,
            region.BottomRight.col);
    }

    internal IEnumerable<(int row, int col, CellValue value)> GetNonEmptyCellValues(IRegion region)
    {
        return _dataStore.GetNonEmptyData(region);
    }

    /// <summary>
    /// The non-empty values in the region in row-major order, without positions.
    /// </summary>
    internal List<CellValue> GetNonEmptyValues(IRegion region)
    {
        return _dataStore.GetNonEmptyValues(region);
    }

    /// <summary>
    /// Clears all cell values in the region
    /// </summary>
    /// <param name="regions"></param>
    public void ClearCells(IEnumerable<IRegion> regions)
    {
        var cmd = new ClearCellsCommand(regions);
        Sheet.Commands.ExecuteCommand(cmd);
    }

    public void ClearCells(IRegion region) => ClearCells(new[] { region });

    internal CellStoreRestoreData ClearCellsImpl(IEnumerable<IRegion> regionsToClear)
    {
        var restoreData = new CellStoreRestoreData();
        var toClear = regionsToClear.ToList();

        // batched from the start so that emptying spilled cells does not recalculate before the clear
        Sheet.BatchUpdates();
        WipeSpillsIntersecting(toClear);
        restoreData.ValueRestoreData = _dataStore.Clear(toClear);
        restoreData.ValidRestoreData = _validStore.Clear(toClear);
        restoreData.Merge(ClearFormulaImpl(toClear));

        EmitCellsChanged(toClear);
        Sheet.MarkDirty(toClear);
        Sheet.EndBatchUpdates();
        return restoreData;
    }

    internal CellStoreRestoreData InsertRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new CellStoreRestoreData
        {
            ValueRestoreData = _dataStore.InsertRowColAt(index, count, axis),
            FormatRestoreData = _formatStore.InsertRowColAt(index, count, axis),
            TypeRestoreData = _typeStore.InsertRowColAt(index, count, axis),
            ValidRestoreData = _validStore.InsertRowColAt(index, count, axis),
            MergeRestoreData = _mergeStore.InsertRowColAt(index, count, axis),
            SpillRestoreData = _spillStore.InsertRowColAt(index, count, axis),
            FormulaRestoreData = _formulaStore.InsertRowColAt(index, count, axis),
            DependencyManagerRestoreData =
                Sheet.FormulaEngine.DependencyManager.InsertRowColAt(index, count, axis, Sheet.Name)
        };

        return restoreData;
    }

    internal CellStoreRestoreData RemoveRowColAt(int index, int count, Axis axis)
    {
        // a spill whose formula is removed goes with it; its values must go before the stores shift
        IRegion removed = axis == Axis.Row
            ? new RowRegion(index, index + count - 1)
            : new ColumnRegion(index, index + count - 1);
        foreach (var anchor in GetSpillAnchorsIntersecting(removed).ToList())
        {
            if (removed.Contains(anchor.row, anchor.col))
                WipeSpillForAnchor(anchor);
        }

        var restoreData = new CellStoreRestoreData
        {
            ValueRestoreData = _dataStore.RemoveRowColAt(index, count, axis),
            ValidRestoreData = _validStore.RemoveRowColAt(index, count, axis),
            TypeRestoreData = _typeStore.RemoveRowColAt(index, count, axis),
            FormatRestoreData = _formatStore.RemoveRowColAt(index, count, axis),
            MergeRestoreData = _mergeStore.RemoveRowColAt(index, count, axis),
            SpillRestoreData = _spillStore.RemoveRowColAt(index, count, axis),
            FormulaRestoreData = _formulaStore.RemoveRowColAt(index, count, axis),
            DependencyManagerRestoreData =
                Sheet.FormulaEngine.DependencyManager.RemoveRowColAt(index, count, axis, Sheet.Name)
        };

        return restoreData;
    }

    internal CellStoreRestoreData CopyImpl(IRegion fromRegion, IRegion toRegion, CopyOptions options)
    {
        return CopyImpl(new SheetRange(Sheet, fromRegion), toRegion, options);
    }

    /// <summary>
    /// Copies the cells in <paramref name="from"/>, which may be on another sheet of the workbook, into this
    /// store so that the source's top-left lands on the top-left of <paramref name="toRegion"/>. The whole of
    /// <paramref name="toRegion"/> is cleared first for whatever is being copied.
    /// </summary>
    internal CellStoreRestoreData CopyImpl(SheetRange from, IRegion toRegion, CopyOptions options)
    {
        var restoreData = new CellStoreRestoreData();
        var fromRegion = from.Region.GetIntersection(from.Sheet.Region);
        if (fromRegion == null)
            return restoreData;

        var fromCells = from.Sheet.Cells;
        toRegion = toRegion.GetIntersection(Sheet.Region) ?? toRegion;
        var dr = toRegion.Top - fromRegion.Top;
        var dc = toRegion.Left - fromRegion.Left;

        Sheet.BatchUpdates();

        if (options.CopyValues)
        {
            // collect before clearing: the source may overlap the target. Cells another formula spilled
            // into are left to that formula when it is copied too; a values-only copy takes them as values.
            var values = fromCells._dataStore.GetNonEmptyData(fromRegion)
                .Where(v => !options.CopyFormula || !fromCells.IsSpilledCell(v.row, v.col))
                .ToList();
            var valid = fromCells._validStore.GetNonEmptyData(fromRegion).ToList();
            WipeSpillsIntersecting(toRegion);
            restoreData.ValueRestoreData = _dataStore.Clear([toRegion]);
            restoreData.ValidRestoreData = _validStore.Clear([toRegion]);
            restoreData.Merge(ClearFormulaImpl([toRegion]));
            foreach (var v in values)
                if (Sheet.Region.Contains(v.row + dr, v.col + dc))
                    _dataStore.Set(v.row + dr, v.col + dc, v.data);
            foreach (var v in valid)
                if (Sheet.Region.Contains(v.row + dr, v.col + dc))
                    _validStore.Set(v.row + dr, v.col + dc, v.data);
        }

        if (options.CopyFormula)
            // when values were copied the target's originals are already recorded above, so the formula
            // step must not append later (copied) values that would win on restore
            restoreData.Merge(CopyFormulaImpl(from.Sheet, fromRegion, toRegion, options.FormulaReferences,
                recordValueRestore: !options.CopyValues));

        if (options.CopyFormat)
            restoreData.Merge(CopyFormatImpl(from.Sheet, fromRegion, toRegion));

        if (options.CopyMerges)
            restoreData.Merge(CopyMergesImpl(from.Sheet, fromRegion, toRegion));

        if (options.CopyTypes)
            restoreData.Merge(CopyTypesImpl(from.Sheet, fromRegion, toRegion));

        Sheet.MarkDirty(toRegion);
        EmitCellsChanged(toRegion);
        Sheet.EndBatchUpdates();
        return restoreData;
    }

    internal CellStoreRestoreData CopyFormatImpl(IRegion fromRegion, IRegion toRegion) =>
        CopyFormatImpl(Sheet, fromRegion, toRegion);

    private CellStoreRestoreData CopyFormatImpl(Sheet fromSheet, IRegion fromRegion, IRegion toRegion)
    {
        // Here we create a blank format store and then fill it with the 
        // values that we sample from the source sheet.
        // We then perform the copy inside this new format store and remove the original data.
        // This is then added to the sheet's format store.
        var emptyPalette = new MergeRegionDataStore<CellFormat>();
        var fixedFromRegion = fromRegion.GetIntersection(fromSheet.Region) as Region;
        foreach (var position in fixedFromRegion!)
        {
            var format = fromSheet.GetFormat(position.row, position.col);
            if (Sheet.Protection.IsEnforced)
                format.RemoveLock();
            emptyPalette.Add(new Region(position.row, position.col), format);
        }

        emptyPalette.Copy(fromRegion, toRegion.TopLeft);
        var toClear = fromRegion.Break(toRegion);
        foreach (var region in toClear)
            emptyPalette.Clear(region);


        var restoreData = CutFormatImpl(toRegion);
        foreach (var data in emptyPalette.GetAllDataRegions())
        {
            var region = data.Region.GetIntersection(Sheet.Region);
            if (region != null)
                restoreData.Merge(MergeFormatImpl(region, data.Data));
        }

        return restoreData;
    }

    private CellStoreRestoreData CopyTypesImpl(Sheet fromSheet, IRegion fromRegion, IRegion toRegion)
    {
        var dr = toRegion.Top - fromRegion.Top;
        var dc = toRegion.Left - fromRegion.Left;
        var types = fromSheet.Cells._typeStore.GetDataRegions(fromRegion)
            .Select(x => (x.Data, Region: fromRegion.GetIntersection(x.Region)!))
            .ToList();

        var restoreData = new CellStoreRestoreData { TypeRestoreData = _typeStore.Clear(toRegion) };
        foreach (var (type, region) in types)
        {
            region.Shift(dr, dc);
            var clipped = region.GetIntersection(Sheet.Region);
            if (clipped != null)
                restoreData.TypeRestoreData.Merge(_typeStore.Add(clipped, type));
        }

        return restoreData;
    }

    /// <summary>
    /// Returns true if any cell format in the region is readonly.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public bool ContainsReadOnly(IRegion region)
    {
        foreach (var interval in Sheet.Rows.Formats.GetIntervals(region.Top, region.Bottom))
            if (interval.Data.IsReadOnly == true)
                return true;

        foreach (var interval in Sheet.Columns.Formats.GetIntervals(region.Left, region.Right))
            if (interval.Data.IsReadOnly == true)
                return true;

        foreach (var cellFormat in _formatStore.GetData(region))
            if (cellFormat.IsReadOnly == true)
                return true;

        return false;
    }

    /// <summary>
    /// Restores the internal storage state by redoing any actions that caused the internal data to change.
    /// Fires events for the changed data.
    /// </summary>
    /// <param name="restoreData"></param>
    internal void Restore(CellStoreRestoreData restoreData)
    {
        Sheet.BatchUpdates();

        // Set formula through this function so we add the formula back in to the dependency graph
        Sheet.FormulaEngine.DependencyManager.Restore(restoreData.DependencyManagerRestoreData);

        _formulaStore.Restore(restoreData.FormulaRestoreData);
        // a formula that is gone after the restore takes its spill with it
        foreach (var removed in restoreData.FormulaRestoreData.DataRemoved)
        {
            if (_formulaStore.Get(removed.row, removed.col) == null)
                WipeSpillForAnchor(new CellPosition(removed.row, removed.col));
        }

        _validStore.Restore(restoreData.ValidRestoreData);
        _typeStore.Restore(restoreData.TypeRestoreData);
        _dataStore.Restore(restoreData.ValueRestoreData);
        _formatStore.Restore(restoreData.FormatRestoreData);
        _mergeStore.Restore(restoreData.MergeRestoreData);
        _spillStore.Restore(restoreData.SpillRestoreData);

        foreach (var pt in restoreData.ValueRestoreData.DataRemoved)
        {
            Sheet.MarkDirty(pt.row, pt.col);
            EmitCellChanged(pt.row, pt.col);
        }

        foreach (var region in restoreData.GetAffectedRegions())
        {
            Sheet.MarkDirty(region);
        }

        Sheet.EndBatchUpdates();
    }


    /// <summary>
    /// The <see cref="SheetCell"/> at position row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public SheetCell this[int row, int col]
    {
        get { return new SheetCell(row, col, Sheet); }
    }

    /// <summary>
    /// The <see cref="SheetCell"/> at the address given. If the address is not valid for a cell, null is returned.
    /// </summary>
    /// <param name="cellAddress"></param>
    public SheetCell? this[string cellAddress]
    {
        get
        {
            if (string.IsNullOrEmpty(cellAddress))
                return null;

            var rangeStrFormula = $"={cellAddress}";
            var evaluatedValue =
                Sheet.FormulaEngine.EvaluateFormula(Sheet.FormulaEngine.ParseFormula(rangeStrFormula, Sheet.Name),
                    resolveReferences: false);
            if (evaluatedValue.ValueType == CellValueType.Reference)
            {
                var reference = evaluatedValue.GetValue<Reference>();
                if (reference?.Kind == ReferenceKind.Cell)
                    return new SheetCell(reference.Region!.Top, reference.Region!.Left, Sheet);
            }

            return null;
        }
    }
}