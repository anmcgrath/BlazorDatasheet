namespace BlazorDatasheet.Core.Formats;

public interface IReadonlyCellFormat
{
    /// <summary>
    /// CSS font-weight
    /// </summary>
    public string? FontWeight { get; }

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
    /// The name of displayed inside the cell
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