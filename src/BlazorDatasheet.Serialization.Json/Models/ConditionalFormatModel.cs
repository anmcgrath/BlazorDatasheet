using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Serialization.Json.Models;

public class ConditionalFormatModel
{
    [JsonPropertyName("sqref")] public string RegionString { get; set; }
    public ConditionalFormatAbstractBase Rule { get; set; }
}