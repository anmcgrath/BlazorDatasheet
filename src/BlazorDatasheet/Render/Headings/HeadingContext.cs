using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Render.Headings;

public struct HeadingContext
{
    public int Index { get; }
    public string? Heading { get; }
    public TextAlign Alignment { get; }

    public HeadingContext(int index, string? heading, TextAlign alignment = TextAlign.Start)
    {
        Index = index;
        Heading = heading;
        Alignment = alignment;
    }
}