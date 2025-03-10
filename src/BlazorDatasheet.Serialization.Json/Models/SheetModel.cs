namespace BlazorDatasheet.Serialization.Json.Models;

internal class SheetModel
{
    public List<RowModel> Rows { get; set; } = new();
    public List<ColumnModel> Columns { get; set; } = new();
    public List<DataRegion<int>> CellFormats { get; set; } = new();
    public List<DataRegion<bool>> Merges { get; set; } = new();
    public int NumRows { get; set; }
    public int NumCols { get; set; }
    public List<ConditionalFormatModel> ConditionalFormats { get; set; } = new();
}