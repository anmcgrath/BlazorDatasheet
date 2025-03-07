using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.Data;

public class SheetRow
{
    private readonly Sheet _sheet;
    public int Row { get; }
    public int RowIndex { get; }
    public string? Heading => _sheet.Rows.GetHeading(RowIndex);
    public double Height => _sheet.Rows.GetPhysicalHeight(RowIndex);
    public NonEmptyRowCellCollection NonEmptyCells { get; }

    public SheetRow(int rowIndex, Sheet sheet)
    {
        _sheet = sheet;
        RowIndex = rowIndex;
        Row = rowIndex + 1;
        NonEmptyCells = new NonEmptyRowCellCollection(rowIndex, sheet);
    }
}