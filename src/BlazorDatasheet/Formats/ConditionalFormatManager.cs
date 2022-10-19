using System.Runtime.CompilerServices;
using BlazorDatasheet.Data;
using BlazorDatasheet.Data.Events;
using BlazorDatasheet.Render;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatManager
{
    private readonly Sheet _sheet;

    private List<ConditionalFormat> _registered = new();
    public Dictionary<(int row, int id), Format?> _cachedFormats = new();

    public ConditionalFormatManager(Sheet sheet)
    {
        _sheet = sheet;
        _sheet.CellsChanged += HandleCellsChanged;
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a range, if the conditional formatting exists.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void Apply(ConditionalFormat conditionalFormat, IFixedSizeRange? range)
    {
        if (range == null)
            return;
        if (!_registered.Contains(conditionalFormat))
            _registered.Add(conditionalFormat);
        conditionalFormat.AddRange(range);
        ComputeAllAndCache();
    }

    /// <summary>
    /// Applies conditional format to the whole sheet
    /// </summary>
    /// <param name="key"></param>
    public void Apply(ConditionalFormat format)
    {
        Apply(format, _sheet.Range);
    }

    private void HandleCellsChanged(object? sender, IEnumerable<ChangeEventArgs> args)
    {
    }

    public void ComputeAndCache(int row, int col)
    {
        var conditionalFormatsApplied = GetFormatsAppliedToPosition(row, col);
        var cell = _sheet.GetCell(row, col);
        foreach (var cf in conditionalFormatsApplied)
        {
            if (!cf.FormattingDependentOnCells)
            {
                // only apply to itself
                var format = this.computeResultOfCf(cell, cf, null);
                this.CacheFormat();
                // Apply to self and then all other cells attached to format
            }
            else
            {
                var cellsInCf = GetCellsInFormat(cf);
                t
            }

            
        }
    }

    private IEnumerable<Cell> GetCellsInFormat(ConditionalFormat format)
    {
        return _sheet.GetCellsInRanges(format.Ranges);
    }

    public void ComputeAllAndCache()
    {
        _cachedFormats.Clear();
        // Gather the cells that are applied to each conditional format, if appropriate
        var cfDependentCellCache = new Dictionary<ConditionalFormat, List<Cell>>();

        foreach (var conditionalFormat in _registered)
        {
            if (conditionalFormat.FormattingDependentOnCells)
                cfDependentCellCache.Add(conditionalFormat, _sheet.GetCellsInRanges(conditionalFormat.Ranges).ToList());
        }

        foreach (var posn in _sheet.Range)
        {
            Format? initialFormat = null;
            var conditionalFormatsAppliedToCell = GetFormatsAppliedToPosition(posn.Row, posn.Col);
            var cell = _sheet.GetCell(posn);
            foreach (var conditionalFormat in conditionalFormatsAppliedToCell)
            {
                var apply = conditionalFormat.RuleSucceed(cell);
                if (!apply)
                    continue;

                cfDependentCellCache.TryGetValue(conditionalFormat, out var cells);
                var result = computeResultOfCf(cell, conditionalFormat, cells);
                if (result != null)
                {
                    if (initialFormat == null)
                        initialFormat = result;
                    else
                        initialFormat.Merge(result);
                }

                if (apply && conditionalFormat.StopIfTrue)
                    break;
            }

            if (initialFormat != null)
                CacheFormat(posn.Row, posn.Col, initialFormat);
        }
    }

    private Format? computeResultOfCf(Cell cell, ConditionalFormat conditionalFormat, List<Cell>? cells)
    {
        if (conditionalFormat.FormattingDependentOnCells)
        {
            return conditionalFormat.FormatFuncDependent.Invoke(cell, cells);
        }
        else
        {
            return conditionalFormat.FormatFunc.Invoke(cell);
        }
    }

    private IEnumerable<ConditionalFormat> GetFormatsAppliedToPosition(int row, int col)
    {
        return _registered
            .Where(x => x.Ranges.Any(x => x.Contains(row, col)));
    }

    private void OnRowInserted(int rowIndex)
    {
    }

    private void OnCellChanged(int row, int col)
    {
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a range.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void Apply(ConditionalFormat format, int row, int col)
    {
        Apply(format, new Range(row, col));
    }

    private void CacheFormat(int row, int col, Format? format)
    {
        if (!_cachedFormats.ContainsKey((row, col)))
            _cachedFormats.Add((row, col), format);
        _cachedFormats[(row, col)] = format;
    }

    public Format? GetFormat(int row, int col)
    {
        var tuple = (row, col);
        if (!_cachedFormats.ContainsKey(tuple))
            return null;
        return _cachedFormats[(row, col)];
    }
}