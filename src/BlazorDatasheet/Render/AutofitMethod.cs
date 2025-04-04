namespace BlazorDatasheet.Render;

[Flags]
public enum AutofitMethod
{
    None = 0,

    /// <summary>
    /// Auto-fit the row height to the content.
    /// </summary>
    Row = 1,

    /// <summary>
    /// Auto-fit the column width to the content.
    /// </summary>
    Column = 2,
}