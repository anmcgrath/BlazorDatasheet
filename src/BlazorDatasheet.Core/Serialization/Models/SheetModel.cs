using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Serialization.Json.Models;

internal class SheetModel
{
    public string Name { get; set; }
    public List<RowModel> Rows { get; set; } = new();
    public List<ColumnModel> Columns { get; set; } = new();
    public List<DataRegionModel<int>> CellFormats { get; set; } = new();
    public List<DataRegionModel<bool>> Merges { get; set; } = new();
    public List<DataRegionModel<string>> Types { get; set; } = new();
    public List<DataRegionModel<IDataValidator>> Validators { get; set; } = new();
    public int NumRows { get; set; }
    public int NumCols { get; set; }
    public List<ConditionalFormatModel> ConditionalFormats { get; set; } = new();
    public int DefaultWidth { get; set; }
    public int DefaultHeight { get; set; }
}