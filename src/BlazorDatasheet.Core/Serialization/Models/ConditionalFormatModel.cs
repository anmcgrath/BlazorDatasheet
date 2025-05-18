using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Serialization.Models;

internal class ConditionalFormatModel
{
    public string RegionString { get; set; } = string.Empty;
    public string RuleType { get; set; } = string.Empty;
    public ConditionalFormatAbstractBase Rule { get; set; } = null!;
}