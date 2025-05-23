namespace BlazorDatasheet.Core.Serialization.Models;

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