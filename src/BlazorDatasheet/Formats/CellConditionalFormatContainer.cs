using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats;

/// <summary>
/// Contains the list of formats calculated on a cell.
/// </summary>
internal class CellConditionalFormatContainer
{
    private readonly SortedList<int, ConditionalFormatResult> Results = new();

    /// <summary>
    /// Sets the format calculated by the conditional format with the unique id conditionalFormatIndex
    /// </summary>
    /// <param name="format"></param>
    /// <param name="conditionalFormatIndex"></param>
    internal void SetResult(ConditionalFormatResult result)
    {
        if (!Results.ContainsKey(result.Index))
            Results.Add(result.Index, result);
        else
            Results[result.Index] = result;
    }

    /// <summary>
    /// Merges conditional formats in the order that they were applied
    /// </summary>
    /// <returns></returns>
    internal Format? GetMergedFormat()
    {
        Format? InitialFormat = null;
        var results = Results.Values;
        foreach (var result in results)
        {
            if (InitialFormat == null)
                InitialFormat = result.Format;
            else
                InitialFormat.Merge(result.Format);
        }

        return InitialFormat;
    }
}