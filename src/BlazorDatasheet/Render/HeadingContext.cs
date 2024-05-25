namespace BlazorDatasheet.Render;

public class HeadingContext
{
    public int Id { get; }
    public string? Heading { get; }

    public HeadingContext(int id, string? heading)
    {
        Id = id;
        Heading = heading;
    }
}