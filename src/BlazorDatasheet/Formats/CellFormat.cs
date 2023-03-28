using BlazorDatasheet.Data.SpatialDataStructures;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Formats;

public class CellFormat : IMergeable<CellFormat>
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
    /// The icon displayed inside the cell
    /// </summary>
    public RenderFragment? Icon { get; set; }
    /// <summary>
    /// The icon's CSS color
    /// </summary>
    public string? IconColor { get; set; }

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
            IconColor = IconColor
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
        if (!String.IsNullOrEmpty(format.BackgroundColor))
            this.BackgroundColor = format.BackgroundColor;
        if (!String.IsNullOrEmpty(format.ForegroundColor))
            this.ForegroundColor = format.ForegroundColor;
        if (!String.IsNullOrEmpty(format.BackgroundColor))
            this.FontWeight = format.FontWeight;
        if (format.Icon != null)
            this.Icon = format.Icon;
        if (!String.IsNullOrEmpty(format.StringFormat))
            this.StringFormat = format.StringFormat;
        if (!String.IsNullOrEmpty(format.IconColor))
            this.IconColor = format.IconColor;
    }
}