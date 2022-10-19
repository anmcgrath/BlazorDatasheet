using BlazorDatasheet.Data;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Render;

public class ConditionalFormat
{
    /// <summary>
    /// Whether the formatting is dependent on other cells.
    /// This is set by the constructor and can potentially speed things up
    /// for the CF evaluation as the manager doesn't have to gather the other cells
    /// that the CF applies to.
    /// </summary>
    internal bool FormattingDependentOnCells => FormatFuncDependent != null;

    public Func<Cell, bool> Rule { get; private set; }
    public bool StopIfTrue { get; set; }

    /// <summary>
    /// The function that returns the conditional format, based on the cell's value
    /// and all other cell values in the sheet
    /// </summary>
    public Func<Cell, IEnumerable<Cell>, Format>? FormatFuncDependent { get; private set; }

    /// <summary>
    /// The function that returns the conditional format, based only on one cell's value
    /// </summary>
    public Func<Cell, Format>? FormatFunc { get; }
    
    private readonly List<IFixedSizeRange?> _ranges;
    public IReadOnlyCollection<IFixedSizeRange?> Ranges => _ranges;

    private ConditionalFormat()
    {
        _ranges = new List<IFixedSizeRange?>();
    }

    /// <summary>
    /// Creates a conditional format that can be applied to cells in the sheet.
    /// The conditional format is evaluated on cell re-render and should be based on either the
    /// specific cell's value or the values of all cells that the conditional formats apply to.
    /// </summary>
    /// <param name="rule">The rule determining whether the conditional format is applied</param>
    /// <param name="formatFuncDependent">The function determining the actual format to apply, based on both the single cell's
    /// value and all cells that the conditional format applies to.</param>
    public ConditionalFormat(Func<Cell, bool> rule, Func<Cell, IEnumerable<Cell>, Format> formatFuncDependent) : this()
    {
        Rule = rule;
        FormatFuncDependent = formatFuncDependent;
    }

    /// <summary>
    /// Creates a conditional format that can be applied to cells in the sheet.
    /// The conditional format is evaluated on cell re-render and should be based on either the
    /// specific cell's value or the values of all cells that the conditional formats apply to.
    /// </summary>
    /// <param name="rule">The rule determining whether the conditional format is applied</param>
    /// <param name="formatFunc">The function determining the actual format to apply, based on both the single cell's
    /// value and all cells that the conditional format applies to.</param>
    public ConditionalFormat(Func<Cell, bool> rule, Func<Cell, Format> formatFunc) : this()
    {
        Rule = rule;
        FormatFunc = formatFunc;
    }

    /// <summary>
    /// Apply the conditional format to the cells in the range specified
    /// </summary>
    /// <param name="range"></param>
    public void AddRange(IFixedSizeRange? range)
    {
        _ranges.Add(range);
    }

    /// <summary>
    /// Determine whether the format should be applied to the cell
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public bool RuleSucceed(Cell cell)
    {
        return Rule.Invoke(cell);
    }

    /// <summary>
    /// Determines whether a cell is linked to this conditional format.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsAppliedTo(int row, int col)
    {
        return _ranges.Any(x => x.Contains(row, col));
    }
}