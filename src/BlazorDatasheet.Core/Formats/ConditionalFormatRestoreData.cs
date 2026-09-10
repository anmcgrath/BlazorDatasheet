using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Formats;

internal class ConditionalFormatRestoreData
{
    public RegionRestoreData<ConditionalFormatAbstractBase> Regions { get; init; } = new();

    /// <summary>
    /// The formula text of each formula conditional format before its references were rewritten.
    /// </summary>
    public List<(FormulaConditionalFormat Cf, string FormulaText)> Formulas { get; init; } = new();
}
