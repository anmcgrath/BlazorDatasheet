using System.Diagnostics;
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
    public string? NumberFormat { get; set; }

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
    /// Left border
    /// </summary>
    public Border? BorderLeft { get; set; }

    /// <summary>
    /// Right border
    /// </summary>
    public Border? BorderRight { get; set; }

    /// <summary>
    /// Top border
    /// </summary>
    public Border? BorderTop { get; set; }

    /// <summary>
    /// Bottom border
    /// </summary>
    public Border? BorderBottom { get; set; }

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
            NumberFormat = NumberFormat,
            IconColor = IconColor,
            TextAlign = TextAlign,
            IsReadOnly = IsReadOnly,
            BorderBottom = BorderBottom?.Clone(),
            BorderLeft = BorderLeft?.Clone(),
            BorderRight = BorderRight?.Clone(),
            BorderTop = BorderTop?.Clone()
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
        if (!string.IsNullOrEmpty(format.FontWeight))
            this.FontWeight = format.FontWeight;
        if (!string.IsNullOrEmpty(format.TextAlign))
            this.TextAlign = format.TextAlign;
        if (format.Icon != null)
            this.Icon = format.Icon;
        if (!string.IsNullOrEmpty(format.NumberFormat))
            this.NumberFormat = format.NumberFormat;
        if (!string.IsNullOrEmpty(format.IconColor))
            this.IconColor = format.IconColor;
        if (format.IsReadOnly.HasValue)
            this.IsReadOnly = format.IsReadOnly;

        MergeBorders(format);
    }

    private void MergeBorders(CellFormat format)
    {
        if (format.BorderBottom != null)
        {
            if (this.BorderBottom == null)
                this.BorderBottom = format.BorderBottom.Clone();
            else
                this.BorderBottom?.Merge(format.BorderBottom);
        }

        if (format.BorderLeft != null)
        {
            if (this.BorderLeft == null)
                this.BorderLeft = format.BorderLeft.Clone();
            else
                this.BorderLeft?.Merge(format.BorderLeft);
        }

        if (format.BorderRight != null)
        {
            if (this.BorderRight == null)
                this.BorderRight = format.BorderRight.Clone();
            else
                this.BorderRight?.Merge(format.BorderRight);
        }

        if (format.BorderTop != null)
        {
            if (this.BorderTop == null)
                this.BorderTop = format.BorderTop.Clone();
            else
                this.BorderTop?.Merge(format.BorderTop);
        }
    }

    public bool HasBorder() => BorderTop != null &&
                               BorderBottom != null &&
                               BorderRight != null &&
                               BorderLeft != null;


    public bool Equals(CellFormat? other)
    {
        return this.FontWeight == other?.FontWeight &&
               this.BackgroundColor == other?.BackgroundColor &&
               this.IconColor == other?.IconColor &&
               this.IsReadOnly == other?.IsReadOnly &&
               this.ForegroundColor == other?.ForegroundColor &&
               this.NumberFormat == other?.NumberFormat &&
               this.TextAlign == other?.TextAlign &&
               this.Icon == other?.Icon &&
               this.BorderBottom == other?.BorderBottom &&
               this.BorderLeft == other?.BorderLeft &&
               this.BorderRight == other?.BorderRight &&
               this.BorderTop == other?.BorderTop;
    }
}