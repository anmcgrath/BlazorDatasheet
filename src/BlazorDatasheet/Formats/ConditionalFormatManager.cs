using BlazorDatasheet.Data;
using BlazorDatasheet.Render;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Formats;

public class ConditionalFormatManager
{
    private readonly Sheet _sheet;
    private readonly Dictionary<string, ConditionalFormat> _conditionalFormats = new();
    internal IReadOnlyDictionary<string, ConditionalFormat> ConditionalFormats => _conditionalFormats;

    /// <summary>
    /// The list of formats that have been applied to the sheet (not just registered)
    /// </summary>
    private List<ConditionalFormat> _appliedFormats = new();

    public ConditionalFormatManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Adds a conditional formatting object to the sheet. Must be applied by setting ApplyConditionalFormat
    /// </summary>
    /// <param name="key">A unique ID identifying the conditional format</param>
    /// <param name="conditionalFormat"></param>
    public void Register(string key, ConditionalFormat conditionalFormat)
    {
        this._conditionalFormats.Add(key, conditionalFormat);
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a range, if the conditional formatting exists.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void Apply(string key, IRange? range)
    {
        if (!ConditionalFormats.ContainsKey(key))
            return;
        var cf = ConditionalFormats[key];
        if (!_appliedFormats.Contains(cf))
            _appliedFormats.Add(cf);
        cf.AddRange(range);
    }

    /// <summary>
    /// Applies conditional format to the whole sheet
    /// </summary>
    /// <param name="key"></param>
    public void Apply(string key)
    {
        Apply(key, new AllRange());
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a range.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void Apply(string key, int row, int col)
    {
        Apply(key, new Range(row, col));
    }

    private IEnumerable<Cell> getCellsWithConditionalFormatApplied(ConditionalFormat conditionalFormat)
    {
        var cellsWithConditionalFormat = conditionalFormat
                                         .Ranges
                                         .Select(x => x.GetIntersection(_sheet.Range))
                                         .SelectMany(x => x.AsEnumerable())
                                         .Select(x => _sheet.GetCell(x));

        return cellsWithConditionalFormat;
    }

    /// <summary>
    /// Determines the formatting after applying any conditional formats that exist for the cell
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public Format? CalculateFormat(int row, int col)
    {
        // Somehow we need to inform the cells of the other cells that the conditional format
        // applies to.... gah!
        if (!_appliedFormats.Any())
            return null;

        // Would be good to cache this somewhere
        var appliedConditionalFormats =
            _appliedFormats.Where(x => x.IsAppliedTo(row, col));


        var cell = _sheet.GetCell(row, col);

        Format? initialFormat = null;

        foreach (var conditionalFormat in appliedConditionalFormats)
        {
            var apply = conditionalFormat.Rule.Invoke(cell);
            if (!apply)
                continue;

            Format? calculatedFormat;
            if (conditionalFormat.FormattingDependentOnCells)
            {
                calculatedFormat =
                    conditionalFormat.FormatFuncDependent.Invoke(
                        cell, getCellsWithConditionalFormatApplied(conditionalFormat));
            }
            else
            {
                calculatedFormat = conditionalFormat.FormatFunc.Invoke(cell);
            }

            if (initialFormat == null)
                initialFormat = calculatedFormat;
            else
                initialFormat.Merge(calculatedFormat);
        }

        return initialFormat;
    }
}