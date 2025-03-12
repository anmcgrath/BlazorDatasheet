using System.Text.Json.Serialization;

namespace BlazorDatasheet.Serialization.Json.Models;

public class DataRegionModel<T>
{
    [JsonPropertyName("sqref")] public string RegionString { get; set; }
    [JsonPropertyName("v")] public T Value { get; set; }

    public DataRegionModel(string regionString, T value)
    {
        RegionString = regionString;
        Value = value;
    }
}