using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Serialization.Json.Constants;

namespace BlazorDatasheet.Core.Serialization.Models;

internal class RowModel
{
    [JsonPropertyName(JsonConstants.RowIndex)]
    public int RowIndex { get; set; }

    public string? Heading { get; set; }
    public double? Height { get; set; }
    public List<CellModel> Cells { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Hidden { get; set; }

    [JsonPropertyName(JsonConstants.FormatIndex)]
    public int? FormatIndex { get; set; }
}