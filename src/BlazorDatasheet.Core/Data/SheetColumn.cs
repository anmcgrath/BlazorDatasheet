using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

public class SheetColumn : SheetRange
{
    internal SheetColumn(Sheet sheet, int columnIndex) : base(sheet, new ColumnRegion(columnIndex))
    {
    }
}