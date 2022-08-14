namespace BlazorDatasheet.Model;

public class Format
{
    public string FontWeight { get; set; }
    public string BackgroundColor { get; set; }
    public string ForegroundColor { get; set; }

    public Format Clone()
    {
        return new Format()
        {
            FontWeight = FontWeight,
            BackgroundColor = BackgroundColor,
            ForegroundColor = ForegroundColor
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
    }

    public static Format Default =>
        new Format()
        {
            FontWeight = "normal",
            BackgroundColor = "#ffffff",
            ForegroundColor = "#000000",
        };
}