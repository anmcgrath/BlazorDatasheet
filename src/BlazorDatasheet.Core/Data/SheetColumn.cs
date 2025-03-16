using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Data;

public class SheetColumn
{
    public Sheet Sheet { get; }
    public int Column { get; }
    public int ColIndex { get; }
    public string? Heading => Sheet.Columns.GetHeading(ColIndex);
    public double Width => Sheet.Columns.GetPhysicalWidth(ColIndex);
    public bool Visible => Sheet.Columns.IsVisible(ColIndex);
    public CellFormat? Format => Sheet.Columns.Formats.Get(ColIndex);
    public IReadOnlyList<IFilter> Filters => Sheet.Columns.Filters.Get(ColIndex).Filters;

    public SheetColumn(int colIndex, Sheet sheet)
    {
        Sheet = sheet;
        ColIndex = colIndex;
        Column = colIndex + 1;
    }
}