using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Extensions;

public static class CellFormatExtension
{
    public static string GetStyleString(this CellFormat format)
    {
        var sb = new StyleBuilder();
        sb.AddStyleNotNull("background", format.BackgroundColor);
        sb.AddStyleNotNull("color", format.ForegroundColor);
        sb.AddStyleNotNull("font-weight", format.FontWeight);
        sb.AddStyleNotNull("text-align", format.TextAlign);
        sb.AddStyleNotNull("border-left", format.BorderLeft?.ToString());
        sb.AddStyleNotNull("border-right", format.BorderRight?.ToString());
        sb.AddStyleNotNull("border-top", format.BorderTop?.ToString());
        return sb.ToString();
    }
}