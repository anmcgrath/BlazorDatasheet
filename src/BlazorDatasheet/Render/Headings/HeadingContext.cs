namespace BlazorDatasheet.Render.Headings;

public struct HeadingContext
{
    public int Index { get; }
    public string? Heading { get; }

    public HeadingContext(int index, string? heading)
    {
        Index = index;
        Heading = heading;
    }
}