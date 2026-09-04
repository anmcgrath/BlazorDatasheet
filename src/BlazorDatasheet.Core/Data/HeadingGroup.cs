namespace BlazorDatasheet.Core.Data;

/// <summary>
/// A labelled group spanning a contiguous range of rows/columns. Stored by reference so that
/// two adjacent groups with the same label remain distinct.
/// </summary>
public class HeadingGroup
{
    public string Label { get; }

    public HeadingGroup(string label)
    {
        Label = label;
    }
}
