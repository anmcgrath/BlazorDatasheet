using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data.Filter;

namespace BlazorDatasheet.Serialization.Json.Models;

internal class ColumnModel
{
    public string? Heading { get; set; }
    public double? Width { get; set; }
    public int ColIndex { get; set; }
    [JsonPropertyName("fi")] public int? FormatIndex { get; set; }

    public List<IFilter> Filters { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Hidden { get; set; }
}