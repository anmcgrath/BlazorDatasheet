using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Serialization.Json.Models;

internal class WorkbookModel
{
    public List<CellFormat> Formats { get; set; } = new();
    public List<SheetModel> Sheets { get; set; } = new();
    public List<Variable> Variables { get; set; } = new();
}