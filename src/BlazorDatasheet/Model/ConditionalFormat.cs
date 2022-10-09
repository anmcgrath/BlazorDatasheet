namespace BlazorDatasheet.Model;

public class ConditionalFormat
{
    public Func<Cell, Cell[], bool> Rule { get; private set; }
    public Func<Cell, Cell[], Format> FormatFunc { get; private set; }
    public bool StopIfTrue { get; set; } = true;
    private readonly List<Range> _ranges;
    public IReadOnlyCollection<Range> Ranges => _ranges;

    /// <summary>
    /// Creates a conditional format that can be applied to cells in the sheet.
    /// The conditional format is evaluated on cell re-render and should be based on either the
    /// specific cell's value or the values of all cells that the conditional formats apply to.
    /// </summary>
    /// <param name="rule">The rule determining whether the conditional format is applied</param>
    /// <param name="formatFunc">The function determining the actual format to apply, based on both the single cell's
    /// value and all cells that the conditional format applies to.</param>
    public ConditionalFormat(Func<Cell, Cell[], bool> rule, Func<Cell, Cell[], Format> formatFunc)
    {
        _ranges = new List<Range>();
        Rule = rule;
        FormatFunc = formatFunc;
    }
    
    /// <summary>
    /// Apply the conditional format to the cells in the range specified
    /// </summary>
    /// <param name="range"></param>
    public void AddRange(Range range)
    {
        _ranges.Add(range);
    }

    /// <summary>
    /// Determines whether a cell is linked to this conditional format.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsActiveForCell(int row, int col)
    {
        return _ranges.Any(x => x.Contains(row, col));
    }
}