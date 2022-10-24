namespace BlazorDatasheet.Data;

public class BRangeCell : BRange
{
    public new object Value
    {
        set => base.Value = value;
        get => base.GetCells().First().GetValue();
    }

    public BRangeCell(Sheet sheet, int row, int col) : base(sheet, new Region(row, col))
    {
    }
}