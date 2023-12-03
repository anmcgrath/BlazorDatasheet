namespace BlazorDatasheet.Formula.Core;

public class ColumnAddress : RangeAddress
{
    public ColumnAddress(int start, int end) : base(0, int.MaxValue, start, end)
    {
    }
}