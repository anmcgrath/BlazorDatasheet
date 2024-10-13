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
                var colRegion = new ColumnRegion(columnFilter.Column);
                var nonEmptyCells = sheet.Cells.GetNonEmptyCellValues(colRegion).ToList();

                if (nonEmptyCells.Count == 0) // all blank
                {
                    if (!filter.IncludeBlanks)
                        hidden.Set(0, sheet.NumRows - 1, true);
                    continue;
                }

                bool inHiddenInterval = false;
                int hiddenStart = 0;

                if (nonEmptyCells.First().row > 0 && !filter.IncludeBlanks)
                    inHiddenInterval = true;

                for (int i = 0; i < nonEmptyCells.Count; i++)
                {
                    var (row, _, cell) = nonEmptyCells[i];

                    // if there were some blanks before this row,
                    // ensure we don't hide them if filter includes blanks
                    if (i > 0 && (row - 1) != nonEmptyCells[i - 1].row)
                    {
                        // we have blanks before
                        if (inHiddenInterval && filter.IncludeBlanks)
                        {
                            hidden.Set(hiddenStart, nonEmptyCells[i - 1].row, true);
                            inHiddenInterval = false;
                        }
                        else if (!inHiddenInterval && !filter.IncludeBlanks)
                        {
                            hidden.Set(nonEmptyCells[i - 1].row + 1, row - 1, true);
                        }
                    }

                    var hideRow = hidden.Get(row) || !filter.Match(cell);
                    if (hideRow && !inHiddenInterval)
                    {
                        hiddenStart = row;
                        inHiddenInterval = true;
                    }

                    if (!hideRow && inHiddenInterval)
                    {
                        hidden.Set(hiddenStart, row - 1, true);
                        inHiddenInterval = false;
                    }
                }

                if (inHiddenInterval)
                {
                    var end = !filter.IncludeBlanks ? sheet.NumRows - 1 : nonEmptyCells.Last().row;
                    hidden.Set(hiddenStart, end, true);
                }
                else if (hiddenStart < sheet.NumRows - 1 && !filter.IncludeBlanks)
                {
                    hidden.Set(nonEmptyCells.Last().row + 1, sheet.NumRows - 1, true);
                }
            }
        }

        return hidden.GetAllIntervals();
    }
}