using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SortRangeCommand : IUndoableCommand
{
    private readonly IRegion _region;
    public IRegion? SortedRegion;
    private readonly List<ColumnSortOptions> _sortOptions;
    public int[] OldIndices = Array.Empty<int>();
    private readonly RegionRestoreData<string> _typeRestoreData = new();

    /// <summary>
    /// Sorts the specified region on values using the specified sort options.
    /// </summary>
    /// <param name="region">The region to sort</param>
    /// <param name="sortOptions">The column sort options, if null the default sort (sort on column 0 ascending) will be used.
    /// If two column values are equal, then the next option will be used for the sort, equivalent to a ThenBy</param>
    public SortRangeCommand(IRegion region, List<ColumnSortOptions>? sortOptions = null)
    {
        _region = region;
        _sortOptions = sortOptions ?? [new(0, true)];
    }

    /// <summary>
    /// Sorts the specified region on values using the specified sort options.
    /// </summary>
    /// <param name="region">The region to sort</param>
    /// <param name="sortOption">The column sort options, if null the default sort (sort on column 0 ascending) will be used.
    /// If two column values are equal, then the next option will be used for the sort, equivalent to a ThenBy</param>
    public SortRangeCommand(IRegion region, ColumnSortOptions sortOption)
    {
        _region = region;
        _sortOptions = new List<ColumnSortOptions> { sortOption };
    }

    public bool Execute(Sheet sheet)
    {
        var store = sheet.Cells.GetCellDataStore();

        var rowCollection = store.GetNonEmptyRowData(_region);
        var rowIndices = new Span<int>(rowCollection.RowIndicies);
        var rowData = new Span<RowData<CellValue>>(rowCollection.Rows);

        if (rowIndices.Length == 0)
            return true;

        rowData.Sort(rowIndices, Comparison);

        SortedRegion = new Region(_region.Top, _region.Top + rowIndices.Length - 1, _region.Left, _region.Right);
        SortedRegion = SortedRegion.GetIntersection(sheet.Region);

        if (SortedRegion == null)
            return true;

        sheet.BatchUpdates();

        var formulaData = sheet.Cells.GetFormulaStore().GetSubStore(_region, false);
        var typeData =
            (ConsolidatedDataStore<string>)sheet.Cells.GetTypeStore().GetSubStore(_region, false);

        // clear any row data that has been shifted (which should be all non-empty rows)
        for (int i = 0; i < rowData.Length; i++)
        {
            var row = rowData[i].Row;
            var rowReg = new Region(row, row, _region.Left, _region.Right);
            sheet.Cells.ClearCellsImpl(new[] { rowReg });
            _typeRestoreData.Merge(sheet.Cells.GetTypeStore().Clear(rowReg));
        }

        for (int i = 0; i < rowData.Length; i++)
        {
            var newRowNo = _region.Top + i;
            var oldRowNo = rowData[i].Row;

            for (int j = 0; j < rowData[i].ColumnIndices.Length; j++)
            {
                var col = rowData[i].ColumnIndices[j];
                var val = rowData[i].Values[j];
                var formula = formulaData.Get(oldRowNo, col);
                var type = typeData.Get(oldRowNo, col);

                sheet.Cells.SetCellTypeImpl(new Region(newRowNo, col), type);

                if (formula == null)
                    sheet.Cells.SetValueImpl(newRowNo, col, val);
                else
                {
                    formula.ShiftReferences((newRowNo - oldRowNo), 0);
                    sheet.Cells.SetFormulaImpl(newRowNo, col, formula);
                }
            }
        }

        sheet.EndBatchUpdates();

        OldIndices = rowIndices.ToArray();

        return true;
    }

    private int Comparison(RowData<CellValue> x, RowData<CellValue> y)
    {
        for (int i = 0; i < _sortOptions.Count; i++)
        {
            var sortOption = _sortOptions[i];
            var xValue = x.GetColumnData(sortOption.ColumnIndex + _region.Left);
            var yValue = y.GetColumnData(sortOption.ColumnIndex + _region.Left);

            if (xValue?.Data == null && yValue?.Data == null)
                continue;

            // null comparisons shouldn't depend on the sort order -
            // null values always end up last.
            if (xValue?.Data == null)
                return 1;
            if (yValue?.Data == null)
                return -1;

            int comparison = xValue.CompareTo(yValue);

            if (comparison == 0)
                continue;

            comparison = sortOption.Ascending ? comparison : -comparison;

            return comparison;
        }

        return 0;
    }

    public bool Undo(Sheet sheet)
    {
        if (SortedRegion == null)
            return true;

        var rowCollection = sheet.Cells.GetCellDataStore().GetRowData(SortedRegion);
        var formulaCollection = sheet.Cells.GetFormulaStore().GetSubStore(SortedRegion, false);
        var typeCollection = (ConsolidatedDataStore<string>)sheet.Cells.GetTypeStore().GetSubStore(SortedRegion);

        sheet.BatchUpdates();
        sheet.Cells.ClearCellsImpl(new[] { SortedRegion });
        sheet.Cells.GetTypeStore().Clear(SortedRegion);

        var rowData = rowCollection.Rows;
        var rowIndices = rowCollection.RowIndicies;
        for (int i = 0; i < OldIndices.Length; i++)
        {
            var newRowNo = OldIndices[i];
            for (int j = 0; j < rowData[i].ColumnIndices.Length; j++)
            {
                var col = rowData[i].ColumnIndices[j];
                var formula = formulaCollection.Get(rowIndices[i], col);
                var type = typeCollection.Get(rowIndices[i], col);
                if (formula == null)
                {
                    var val = rowData[i].Values[j];
                    sheet.Cells.SetValueImpl(newRowNo, col, val);
                }
                else
                {
                    formula.ShiftReferences((newRowNo - rowIndices[i]), 0);
                    sheet.Cells.SetFormulaImpl(newRowNo, col, formula);
                }

                sheet.Cells.SetCellTypeImpl(new Region(newRowNo, col), type);
            }
        }

        var restoreData = new CellStoreRestoreData()
        {
            TypeRestoreData = _typeRestoreData
        };

        sheet.Cells.Restore(restoreData);
        sheet.EndBatchUpdates();

        return true;
    }
}

public class ColumnSortOptions
{
    /// <summary>
    /// The column index, relative to the range being sorted.
    /// </summary>
    public int ColumnIndex { get; set; }

    /// <summary>
    /// Whether to sort in ascending order.
    /// </summary>
    public bool Ascending { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columnIndex"></param>
    /// <param name="ascending"></param>
    public ColumnSortOptions(int columnIndex, bool ascending)
    {
        ColumnIndex = columnIndex;
        Ascending = ascending;
    }
}