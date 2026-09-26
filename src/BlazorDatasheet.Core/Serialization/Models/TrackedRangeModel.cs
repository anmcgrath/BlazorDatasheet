namespace BlazorDatasheet.Core.Serialization.Models;

internal class TrackedRangeModel
{
    public string Key { get; set; } = string.Empty;
    public string Sheet { get; set; } = string.Empty;
    public string RegionString { get; set; } = string.Empty;
    public bool Deleted { get; set; }
}
