namespace BlazorDatasheet.Core.Formats;

public interface IReadonlyCellFormat
{
    /// <summary>Optional BackgroundPattern styling.</summary>
    public CellBackgroundPattern? BackgroundPattern => null;

    /// <summary>Optional CornerFlagTopLeft styling.</summary>
    public CellCornerFlag? CornerFlagTopLeft => null;

    /// <summary>Optional CornerFlagTopRight styling.</summary>
    public CellCornerFlag? CornerFlagTopRight => null;

    /// <summary>Optional CornerFlagBottomLeft styling.</summary>
    public CellCornerFlag? CornerFlagBottomLeft => null;

    /// <summary>Optional CornerFlagBottomRight styling.</summary>
    public CellCornerFlag? CornerFlagBottomRight => null;

    /// <summary>Optional CssClass styling.</summary>
    public string? CssClass => null;

    /// <summary>Application CSS custom properties. The --bds- prefix is reserved.</summary>
    public IReadOnlyDictionary<string, string>? CssVariables => null;

    /// <summary>
    /// CSS font-weight
    /// </summary>
    public string? FontWeight { get; }

    /// <summary>
    /// CSS font-style
    /// </summary>
    public string? FontStyle { get; }

    /// <summary>
    /// CSS text-decoration
    /// </summary>
    public string? TextDecoration { get; }

    /// <summary>
    /// CSS background color
    /// </summary>
    public string? BackgroundColor { get; }

    /// <summary>
    /// CSS color
    /// </summary>
    public string? ForegroundColor { get; }

    /// <summary>
    /// How to format the string when rendered.
    /// </summary>
    public string? NumberFormat { get; }

    /// <summary>
    /// The name of the icon displayed inside the cell. The name is a key into the datasheet's
    /// <c>Icons</c> render fragment registry - if it isn't registered there, nothing is rendered.
    /// </summary>
    public string? Icon { get; }

    /// <summary>
    /// The icon's CSS color
    /// </summary>
    public string? IconColor { get; }

    /// <summary>
    /// Whether the cell's value can be modified by the user.
    /// </summary>
    public bool? IsReadOnly { get; }
    
    /// <summary>
    /// The text wrapping style.
    /// </summary>
    public TextWrapping TextWrap { get; }

    /// <summary>
    /// True if no custom styles are set.
    /// </summary>
    /// <returns></returns>
    public bool IsDefaultFormat();

    /// <summary>
    /// Horizontal text align.
    /// </summary>
    public TextAlign? HorizontalTextAlign { get; }

    /// <summary>
    /// Vertical text align.
    /// </summary>
    public TextAlign? VerticalTextAlign { get; }

    /// <summary>
    /// Left border
    /// </summary>
    public Border? BorderLeft { get; }

    /// <summary>
    /// Right border
    /// </summary>
    public Border? BorderRight { get; }

    /// <summary>
    /// Top border
    /// </summary>
    public Border? BorderTop { get; }

    /// <summary>
    /// Bottom border
    /// </summary>
    public Border? BorderBottom { get; }

    public CellFormat Clone();
}