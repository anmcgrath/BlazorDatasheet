namespace BlazorDatasheet.Events;

public class FormulaChangedEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public string? OldFormula { get; }
    public string? NewFormula { get; }

    public FormulaChangedEventArgs(int row, int col, string? oldFormula, string? newFormula)
    {
        Row = row;
        Col = col;
        OldFormula = oldFormula;
        NewFormula = newFormula;
    }
}