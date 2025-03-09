namespace BlazorDatasheet.Serialization.Json.Models;

internal class SheetModel
{
    public List<RowModel> Rows { get; set; } = new();
    public List<ColumnModel> Columns { get; set; } = new();
    public List<DataRegion<int>> CellFormats { get; set; }
    public List<DataRegion<bool>> Merges { get; set; }
    public int NumRows { get; set; }
    public int NumCols { get; set; }
}