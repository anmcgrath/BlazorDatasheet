using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

public class SortRangeCommand : IUndoableCommand
{
    private readonly IRegion _region;
    private IRegion? _sortedRegion;
    private readonly List<ColumnSortOptions> _sortOptions;
    private int[] oldIndices = Array.Empty<int>();

    /// <summary>
    /// Sorts the specified region on values using the specified sort options.
    /// </summary>
    /// <param name="region">The region to sort</param>
    /// <param name="sortOptions">The column sort options, if null the default sort (sort on column 0 ascending) will be used.</param>
    public SortRangeCommand(IRegion region, List<ColumnSortOptions>? sortOptions = null)
    {
        _region = region;
        _sortOptions = sortOptions ?? new List<ColumnSortOptions>()
            { new(0, true) };
    }

    public bool Execute(Sheet sheet)
    {
        var store = sheet.Cells.GetCellDataStore();
        var rowCollection = store.GetNonEmptyRowData(_region);
        var rowIndices = new Span<int>(rowCollection.RowIndicies);
        var rowData = new Span<RowData<CellValue>>(rowCollection.Rows);
        rowData.Sort(rowIndices, Comparison);

        sheet.BatchUpdates();

        for (int i = 0; i < rowData.Length; i++)
        {
            var newRowNo = _region.Top + i;
            for (int j = 0; j < rowData[i].ColumnIndices.Length; j++)
            {
                var col = rowData[i].ColumnIndices[j];
                var val = rowData[i].Values[j];
                sheet.Cells.SetValue(newRowNo, col, val);
            }
        }

        sheet.EndBatchUpdates();

        oldIndices = rowIndices.ToArray();
        _sortedRegion = new Region(_region.Top, _region.Top + oldIndices.Length, _region.Left, _region.Right);
        _sortedRegion = _sortedRegion.GetIntersection(sheet.Region);

        return true;
    }

    private int Comparison(RowData<CellValue> x, RowData<CellValue> y)
    {
        for (int i = 0; i < _sortOptions.Count; i++)
        {
            var sortOption = _sortOptions[i];
            var xValue = x.GetColumnData(sortOption.ColumnIndex);
            var yValue = y.GetColumnData(sortOption.ColumnIndex);

            int comparison = 0;
            if (xValue != null && yValue != null)
                comparison = xValue.CompareTo(yValue);
            else
                comparison = xValue == null ? -1 : 1;

            if (comparison != 0)
                return sortOption.Ascending ? comparison : -comparison;
        }

        return 0;
    }

    public bool Undo(Sheet sheet)
    {
        if (_sortedRegion == null)
            return true;

        var rowCollection = sheet.Cells.GetCellDataStore().GetRowData(_sortedRegion);
        sheet.BatchUpdates();

        sheet.Cells.GetCellDataStore().Clear(new Region(
            _sortedRegion.Top,
            _sortedRegion.Top + oldIndices.Length,
            _sortedRegion.Left,
            _sortedRegion.Right));

        var rowData = rowCollection.Rows;
        for (int i = 0; i < oldIndices.Length; i++)
        {
            var newRowNo = oldIndices[i] + _region.Top;
            for (int j = 0; j < rowData[i].ColumnIndices.Length; j++)
            {
                var col = rowData[i].ColumnIndices[j];
                var val = rowData[i].Values[j];
                sheet.Cells.SetValue(newRowNo, col, val);
            }
        }

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