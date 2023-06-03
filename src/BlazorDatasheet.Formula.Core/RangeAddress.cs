namespace BlazorDatasheet.Formula.Core;

/// <summary>
/// Evaluated range address - non-relative.
/// </summary>
public class RangeAddress
{
    public RangeAddress(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        RowStart = rowStart;
        RowEnd = rowEnd;
        ColStart = colStart;
        ColEnd = colEnd;
    }

    public int RowStart { get; }
    public int RowEnd { get; }
    public int ColStart { get; }
    public int ColEnd { get; }
}