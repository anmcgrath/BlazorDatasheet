using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Model;

public class Format
{
    public string FontWeight { get; set; }
    public string BackgroundColor { get; set; }
    public string ForegroundColor { get; set; }
    public string? StringFormat { get; set; }
    public RenderFragment? Icon { get; set; }
    public string IconColor { get; set; }

    public Format Clone()
    {
        return new Format()
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
    public void Merge(Format format)
    {
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

    public static Format Default =>
        new Format()
        {
            FontWeight = "normal",
            BackgroundColor = "#ffffff",
            ForegroundColor = "#000000",
            IconColor = "#000000"
        };
}