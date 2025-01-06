using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Events.Formula;

public class CellFormulaChangeEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public string? OldFormula { get; }
    public string? NewFormula { get; }

    public CellFormulaChangeEventArgs(int row, int col, string? oldFormula, string? newFormula)
    {
        Row = row;
        Col = col;
        OldFormula = oldFormula;
        NewFormula = newFormula;
    }
}