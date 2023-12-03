namespace BlazorDatasheet.Formula.Core;

/// <summary>
/// Evaluated cell address - not relative.
/// </summary>
public class CellAddress : RangeAddress
{
    public CellAddress(int rowStart, int colStart) : base(rowStart, rowStart, colStart, colStart)
    {
    }
}