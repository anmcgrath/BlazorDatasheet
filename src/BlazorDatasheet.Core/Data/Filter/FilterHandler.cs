using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Filter;

internal class FilterHandler
{
    /// <summary>
    /// Find the hidden rows from applying a dictionary of column filters
    /// </summary>
    /// <param name="sheet">The <seealso cref="Sheet"/> to apply the filter to</param>
    /// <param name="column">The column index to match values in</param>
    /// <param name="filter">The <seealso cref="ColumnFilter"/> to apply.</param>
    /// <returns></returns>
    internal List<Interval> GetHiddenRows(Sheet sheet, int column, IFilter filter)
        => GetHiddenRows(sheet, [new ColumnFilter(column, [filter])]);

    /// <summary>
    /// Find the hidden rows from applying a collection of column filters.
    /// </summary>
    /// <param name="sheet">The <seealso cref="Sheet"/> to apply the filter to</param>
    /// <param name="columnFilters">A list of column filters, each with a number of filters.</param>
    /// <returns></returns>
    internal List<Interval> GetHiddenRows(Sheet sheet, IEnumerable<ColumnFilter> columnFilters)
    {
        var hidden = new Range1DStore<bool>(false);
        foreach (var columnFilter in columnFilters)
        {
            foreach (var filter in columnFilter.Filters)
            {
                int hiddenStart = 0;
                int hiddenEnd = -1;

                var nonEmptyCells = sheet.Cells.GetNonEmptyCellValues(new ColumnRegion(columnFilter.Column));
                foreach (var (row, col, cell) in nonEmptyCells)
                {
                    if (hidden.Get(row))
                        continue;

                    if (filter.Match(cell))
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
        }

        return hidden.GetAllIntervals();
    }
}