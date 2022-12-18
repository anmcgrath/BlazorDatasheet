namespace BlazorDatasheet.FormulaEngine.Interfaces;

public interface IRange
{
    IEnumerable<double> GetNonEmptyNumbers();
    int RowStart { get; }
    int RowEnd { get; }
    int ColStart { get; }
    int ColEnd { get; }
}