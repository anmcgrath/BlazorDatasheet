using BlazorDatasheet.FormulaEngine.Interfaces;

namespace ExpressionEvaluator;

public class TestCell : ICell
{
    public int Row { get; }
    public int Col { get; }
    public object Value { get; }

    public TestCell(int row, int col, object value)
    {
        Value = value;
        Row = row;
        Col = col;
    }
}