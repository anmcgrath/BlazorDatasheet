namespace BlazorDatasheet.Formula.Core;

public class RowAddress : RangeAddress
{
    public RowAddress(int start, int end) : base(start, end, 0, int.MaxValue)
    {
    }
}