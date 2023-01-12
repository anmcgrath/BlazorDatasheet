namespace BlazorDatasheet.Data;

public class ValueChange
{
    public int Row { get; }
    public int Col { get; }
    public object? Value { get; }
    public string? FormulaString { get; }

    public ValueChange(int row, int col, object? value, string? formulaString)
    {
        Row = row;
        Col = col;
        Value = value;
        FormulaString = formulaString;
    }
}