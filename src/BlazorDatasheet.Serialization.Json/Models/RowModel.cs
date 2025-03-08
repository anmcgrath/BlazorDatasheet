using System.Text.Json.Serialization;

namespace BlazorDatasheet.Serialization.Json.Models;

internal class RowModel
{
    public int RowIndex { get; set; }
    public string? Heading { get; set; }
    public double? Height { get; set; }
    public List<CellModel> Cells { get; set; } = new();
    [JsonPropertyName("fi")] public int FormatIndex { get; set; }
}