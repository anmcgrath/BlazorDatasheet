using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Serialization.Json.Constants;

namespace BlazorDatasheet.Core.Serialization.Json.Models;

internal class ColumnModel
{
    public string? Heading { get; set; }
    public double? Width { get; set; }
    public int ColIndex { get; set; }

    [JsonPropertyName(JsonConstants.FormatIndex)]
    public int? FormatIndex { get; set; }

    public List<IFilter> Filters { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Hidden { get; set; }
}