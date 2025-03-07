namespace BlazorDatasheet.Core.Data;

public class SheetColumn
{
    public Sheet Sheet { get; }
    public int Column { get; }
    public int ColIndex { get; }
    public string? Heading => Sheet.Columns.GetHeading(ColIndex);
    public double Height => Sheet.Columns.GetPhysicalWidth(ColIndex);

    public SheetColumn(int colIndex, Sheet sheet)
    {
        Sheet = sheet;
        ColIndex = colIndex;
        Column = colIndex + 1;
    }
}