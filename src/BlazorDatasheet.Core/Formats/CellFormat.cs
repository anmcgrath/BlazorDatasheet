using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Formats;

public class CellFormat : IMergeable<CellFormat>, IEquatable<CellFormat>
{
    /// <summary>
    /// CSS font-weight
    /// </summary>
    public string? FontWeight { get; set; }

    /// <summary>
    /// CSS background color
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// CSS color
    /// </summary>
    public string? ForegroundColor { get; set; }

    /// <summary>
    /// How to format the string when rendered.
    /// </summary>
    public string? StringFormat { get; set; }

    /// <summary>
    /// The name of displayed inside the cell
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The icon's CSS color
    /// </summary>
    public string? IconColor { get; set; }

    /// <summary>
    /// Whether the cell's value can be modified by the user.
    /// </summary>
    public bool? IsReadOnly { get; set; }


    /// <summary>
    /// CSS text align
    /// </summary>
    public string? TextAlign { get; set; }

    /// <summary>
    /// Returns a new Format object with cloned properties
    /// </summary>
    /// <returns></returns>
    public CellFormat Clone()
    {
        return new CellFormat()
        {
            FontWeight = FontWeight,
            BackgroundColor = BackgroundColor,
            ForegroundColor = ForegroundColor,
            Icon = Icon,
            StringFormat = StringFormat,
            IconColor = IconColor,
            TextAlign = TextAlign,
            IsReadOnly = IsReadOnly
        };
    }

    /// <summary>
    /// Override this format's properties from a format object. This method only overrides if the properties exist on
    /// the overriding format object.
    /// </summary>
    /// <param name="format">The format object that will override properties of this object, if they exist.</param>
    public void Merge(CellFormat? format)
    {
        if (format == null)
            return;
        if (!string.IsNullOrEmpty(format.BackgroundColor))
            this.BackgroundColor = format.BackgroundColor;
        if (!string.IsNullOrEmpty(format.ForegroundColor))
            this.ForegroundColor = format.ForegroundColor;
        if (!string.IsNullOrEmpty(format.BackgroundColor))
            this.FontWeight = format.FontWeight;
        if (!string.IsNullOrEmpty(format.TextAlign))
            this.TextAlign = format.TextAlign;
        if (format.Icon != null)
            this.Icon = format.Icon;
        if (!string.IsNullOrEmpty(format.StringFormat))
            this.StringFormat = format.StringFormat;
        if (!string.IsNullOrEmpty(format.IconColor))
            this.IconColor = format.IconColor;
        if (format.IsReadOnly.HasValue)
            this.IsReadOnly = format.IsReadOnly;
    }

    public bool Equals(CellFormat? other)
    {
        return this.FontWeight == other?.FontWeight &&
               this.BackgroundColor == other?.BackgroundColor &&
               this.IconColor == other?.IconColor &&
               this.IsReadOnly == other?.IsReadOnly &&
               this.ForegroundColor == other?.ForegroundColor &&
               this.StringFormat == other?.StringFormat &&
               this.TextAlign == other?.TextAlign &&
               this.Icon == other?.Icon;
    }
}