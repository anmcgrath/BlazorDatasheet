using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Events.Edit;

public class EditAcceptedEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public object? Value { get; }
    
    public CellFormula? Formula { get; }
    public string? FormulaString { get; }

    public EditAcceptedEventArgs(int row, int col, object? value, CellFormula? formula, string? formulaString)
    {
        Row = row;
        Col = col;
        Value = value;
        Formula = formula;
        FormulaString = formulaString;
    }
}