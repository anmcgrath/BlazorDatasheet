using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Model;

public class Format
{
    public string FontWeight { get; set; }
    public string BackgroundColor { get; set; }
    public string ForegroundColor { get; set; }
    public RenderFragment? Icon { get; set; }

    public Format Clone()
    {
        return new Format()
        {
            FontWeight = FontWeight,
            BackgroundColor = BackgroundColor,
            ForegroundColor = ForegroundColor,
            Icon = Icon
        };
    }

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
    }

    public static Format Default =>
        new Format()
        {
            FontWeight = "normal",
            BackgroundColor = "#ffffff",
            ForegroundColor = "#000000",
        };
}