using System.Text.Json.Serialization;

namespace BlazorDatasheet.Core.Serialization.Json.Models;

internal class DataRegionModel<T>
{
    public string RegionString { get; set; }
    public T Value { get; set; }

    public DataRegionModel(string regionString, T value)
    {
        RegionString = regionString;
        Value = value;
    }
}