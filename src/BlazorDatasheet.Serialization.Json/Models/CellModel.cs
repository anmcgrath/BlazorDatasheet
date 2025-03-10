using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Serialization.Json.Models;

public class CellModel
{
    public CellValue CellValue { get; set; } = CellValue.Empty;
    public string? Formula { get; set; }
    public int ColIndex { get; set; }
    public Dictionary<string, object> MetaData { get; set; } = new();
}