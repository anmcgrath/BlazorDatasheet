using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Filter;

internal class FilterHandler
{
    /// <summary>
    /// Find the hidden rows from applying a dictionary of column filters.
    /// </summary>
    /// <param name="sheet">The <seealso cref="Sheet"/> to apply the filter to</param>
    /// <param name="filters">A Dictionary of filters, with the key being the column index</param>
    /// <returns></returns>
    internal IEnumerable<Interval> GetHiddenRows(Sheet sheet, Dictionary<int, IColumnFilter> filters)
    {
        var hidden = new Range1DStore<bool>(false);
        foreach (var (column, columnFilter) in filters)
        {
            int hiddenStart = 0;
            int hiddenEnd = -1;

            var nonEmptyCells = sheet.Cells.GetNonEmptyCellValues(new ColumnRegion(column));
            foreach (var (row, col, cell) in nonEmptyCells)
            {
                if (hidden.Get(row))
                    continue;

                if (columnFilter.Match(cell))
                {
                    hiddenEnd = row - 1;
                    if (hiddenEnd >= hiddenStart)
                        hidden.Set(hiddenStart, hiddenEnd, true);
                    hiddenStart = row + 1;
                }
            }

            if (hiddenStart >= hiddenEnd && hiddenStart < sheet.NumRows)
                hidden.Set(hiddenStart, sheet.NumRows - 1, true);
        }

        return hidden.GetAllIntervals();
    }
}