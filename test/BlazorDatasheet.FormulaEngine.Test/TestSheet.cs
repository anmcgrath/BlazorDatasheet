using BlazorDatasheet.DataStructures.Sheet;

namespace ExpressionEvaluator;

public class TestSheet : ISheet
{
    private Dictionary<(int row, int col), object> _values = new();

    public object GetCellValue(int row, int col)
    {
        if (_values.ContainsKey((row, col)))
            return _values[(row, col)];

        return null;
    }

    public IRange GetRange(int rowStart, int rowStop, int colStart, int colStop)
    {
        return new TestRange(rowStart, rowStop, colStart, colStop);
    }

    public IRange GetColumnRange(int colStart, int colStop)
    {
        return new TestColRange(colStart, colStop);
    }

    public IRange GetRowRange(int rowStart, int rowStop)
    {
        return new TestRowRange(rowStart, rowStop);
    }

    public bool TrySetCellValue(int row, int col, object value)
    {
        if (!_values.ContainsKey((row, col)))
            _values.Add((row, col), value);
        _values[(row, col)] = value;

        return true;
    }

    public void Pause()
    {
        
    }

    public void Resume()
    {
        
    }
}