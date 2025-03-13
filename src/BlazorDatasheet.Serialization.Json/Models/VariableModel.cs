using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Serialization.Json.Models;

public class VariableModel
{
    public string? Formula { get; set; }
    public CellValue? Value { get; set; }
    public IRegion? Range { get; set; }
    public string? SheetName { get; set; }
}