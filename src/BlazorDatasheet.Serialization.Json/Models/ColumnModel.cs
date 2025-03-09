using System.Text.Json.Serialization;

namespace BlazorDatasheet.Serialization.Json.Models;

public class ColumnModel
{
    public string? Heading { get; set; }
    public double? Width { get; set; }
    public int ColIndex { get; set; }
    [JsonPropertyName("fi")] public int FormatIndex { get; set; }
    public bool Hidden { get; set; }
}