namespace BlazorDatasheet.Model;

public class ConditionalFormat
{
    public Func<Cell, Cell[], bool> Rule { get; private set; }
    public Format Formatting { get; private set; }
    public bool StopIfTrue { get; set; } = true;
    private readonly List<Range> _ranges;
    public IReadOnlyCollection<Range> Ranges => _ranges;

    public ConditionalFormat(Func<Cell, Cell[], bool> rule, Format formatting)
    {
        _ranges = new List<Range>();
        Rule = rule;
        Formatting = formatting;
    }

    public void AddRange(Range range)
    {
        _ranges.Add(range);
    }

    public bool Contains(int row, int col)
    {
        return _ranges.Any(x => x.Contains(row, col));
    }
}