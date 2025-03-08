namespace BlazorDatasheet.Serialization.Json.Models;

internal class SheetModel
{
    public List<RowModel> Rows { get; set; } = new();
    public List<ColumnModel> Columns { get; set; } = new();
    public int NumRows { get; set; }
    public int NumCols { get; set; }

    public SheetModel()
    {
    }
}