namespace BlazorDatasheet.Model;

public class ConditionalFormat
{
    public Func<Cell, Cell[], bool> Rule { get; private set; }
    public Func<Cell, Cell[], Format> FormatFunc { get; private set; }
    public bool StopIfTrue { get; set; } = true;
    private readonly List<Range> _ranges;
    public IReadOnlyCollection<Range> Ranges => _ranges;

    public ConditionalFormat(Func<Cell, Cell[], bool> rule, Func<Cell, Cell[], Format> formatFunc)
    {
        _ranges = new List<Range>();
        Rule = rule;
        FormatFunc = formatFunc;
    }
    
    public void AddRange(Range range)
    {
        _ranges.Add(range);
    }

    public bool IsActiveForCell(int row, int col)
    {
        return _ranges.Any(x => x.Contains(row, col));
    }
}