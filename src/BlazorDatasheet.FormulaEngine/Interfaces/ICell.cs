namespace BlazorDatasheet.FormulaEngine.Interfaces;

public interface ICell
{
    int Row { get; }
    int Col { get; }
    object Value { get; }
}