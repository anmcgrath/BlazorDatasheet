using BlazorDatasheet.Core.Data.Collections;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.Data;

public class SheetRow
{
    public Sheet Sheet { get; }
    public int Row { get; }
    public int RowIndex { get; }
    public bool IsVisible => Sheet.Rows.IsVisible(RowIndex);
    public string? Heading => Sheet.Rows.GetHeading(RowIndex);
    public double Height => Sheet.Rows.GetPhysicalHeight(RowIndex);
    public NonEmptyCellCollection NonEmptyCells { get; }
    public IReadonlyCellFormat Format => Sheet.Rows.Formats.Get(RowIndex) ?? new CellFormat();

    public SheetRow(int rowIndex, Sheet sheet)
    {
        Sheet = sheet;
        RowIndex = rowIndex;
        Row = rowIndex + 1;
        NonEmptyCells = new NonEmptyCellCollection(rowIndex, sheet);
    }
}