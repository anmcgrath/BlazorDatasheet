using BlazorDatasheet.FormulaEngine.Interfaces;

namespace ExpressionEvaluator;

public class TestSheet : ISheet
{
    private Dictionary<(int row, int col), object> _values = new();

    public ICell GetCell(int row, int col)
    {
        if (_values.ContainsKey((row, col)))
            return new TestCell(row, col, _values[(row, col)]);

        return new TestCell(row, col, null);
    }

    public IRange GetRange(int rowStart, int rowStop, int colStart, int colStop)
    {
        return new TestRange(rowStart, rowStop, colStart, colStop);
    }

    public IRange GetColumn(int colStart, int colStop)
    {
        return new TestColRange(colStart, colStop);
    }

    public IRange GetRowRange(int rowStart, int rowStop)
    {
        return new TestRowRange(rowStart, rowStop);
    }

    public void SetValue(int row, int col, object value)
    {
        if (!_values.ContainsKey((row, col)))
            _values.Add((row, col), value);
        _values[(row, col)] = value;
    }
}