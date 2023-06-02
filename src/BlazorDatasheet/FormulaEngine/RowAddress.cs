namespace BlazorDatasheet.FormulaEngine;

public class RowAddress
{
    private readonly int _end;

    public RowAddress(int start)
    {
        Start = start;
    }

    public int Start { get; }

    public int End => _end;
}