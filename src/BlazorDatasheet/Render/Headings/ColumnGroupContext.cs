namespace BlazorDatasheet.Render.Headings;

/// <summary>
/// Passed to the column group heading template.
/// </summary>
public struct ColumnGroupContext
{
    /// <summary>
    /// The first column of the group.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The last column of the group.
    /// </summary>
    public int End { get; }

    public string Label { get; }

    public ColumnGroupContext(int start, int end, string label)
    {
        Start = start;
        End = end;
        Label = label;
    }
}
