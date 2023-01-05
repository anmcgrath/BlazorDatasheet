using BlazorDatasheet.DataStructures.Sheet;

namespace ExpressionEvaluator;

public class TestCell : ICell
{
    public int Row { get; }
    public int Col { get; }
    public object Value { get; }
    public object GetValue() => Value;

    public TestCell(int row, int col, object value)
    {
        Value = value;
        Row = row;
        Col = col;
    }
}