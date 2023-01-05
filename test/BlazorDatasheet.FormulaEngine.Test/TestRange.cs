using BlazorDatasheet.DataStructures.Sheet;

namespace ExpressionEvaluator;

public class TestRange : IRange
{
    public int RowStart { get; }
    public int RowEnd { get; }
    public int ColStart { get; }
    public int ColEnd { get; }

    public TestRange(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        RowStart = rowStart;
        RowEnd = rowEnd;
        ColStart = colStart;
        ColEnd = colEnd;
    }

    public IEnumerable<double> GetNonEmptyNumbers()
    {
        return new List<double> { 1, 2, 3, 4 };
    }
}

public class TestColRange : TestRange, IRange
{
    public TestColRange(int colStart, int colStop) : base(0, int.MaxValue, colStart, colStop)
    {
    }
}

public class TestRowRange : TestRange, IRange
{
    public TestRowRange(int rowStart, int rowEnd) : base(rowStart, rowEnd, 0, int.MaxValue)
    {
    }
}