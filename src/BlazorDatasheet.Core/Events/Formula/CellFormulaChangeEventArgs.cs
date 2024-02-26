using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Events.Formula;

public class CellFormulaChangeEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public CellFormula? OldFormula { get; }
    public CellFormula? NewFormula { get; }

    public CellFormulaChangeEventArgs(int row, int col, CellFormula? oldFormula, CellFormula? newFormula)
    {
        Row = row;
        Col = col;
        OldFormula = oldFormula;
        NewFormula = newFormula;
    }
}