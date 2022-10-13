using BlazorDatasheet.Data;
using BlazorDatasheet.Render;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatManager
{ 
    private readonly Dictionary<string, ConditionalFormat> _conditionalFormats = new();
    internal IReadOnlyDictionary<string, ConditionalFormat> ConditionalFormats => _conditionalFormats;
    private readonly Dictionary<string, Cell[]> _cellsInConditionalFormatCache = new();
    
    /// <summary>
    /// Adds a conditional formatting object to the sheet. Must be applied by setting ApplyConditionalFormat
    /// </summary>
    /// <param name="key">A unique ID identifying the conditional format</param>
    /// <param name="conditionalFormat"></param>
    public void RegisterConditionalFormat(string key, ConditionalFormat conditionalFormat)
    {
        this._conditionalFormats.Add(key, conditionalFormat);
    }
    
    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a range, if the conditional formatting exists.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void ApplyConditionalFormat(string key, Range range)
    {
        if (!ConditionalFormats.ContainsKey(key))
            return;
        var cf = ConditionalFormats[key];
        cf.AddRange(range);
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a range.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void ApplyConditionalFormat(string key, int row, int col)
    {
        ApplyConditionalFormat(key, new Range(row, col));
    }

    /// <summary>
    /// Determines the "final" formatting of a cell by applying any conditional formatting
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    internal Format GetFormat(Cell cell)
    {
        if (!cell.ConditionalFormattingIds.Any())
            return cell.Formatting;

        var format = cell.Formatting.Clone();
        foreach (var id in cell.ConditionalFormattingIds)
        {
            if (!ConditionalFormats.ContainsKey(id))
                continue;
            var conditionalFormat = this.ConditionalFormats[id];
            var cellsWithConditionalFormat = _cellsInConditionalFormatCache[id];
            var apply = conditionalFormat.Rule.Invoke(cell, cellsWithConditionalFormat);
            if (apply)
                format.Merge(conditionalFormat.FormatFunc.Invoke(cell, cellsWithConditionalFormat));
        }

        return format;
    }
}