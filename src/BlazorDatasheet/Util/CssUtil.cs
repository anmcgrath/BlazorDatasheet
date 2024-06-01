using System.Text;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Util;

public static class CssUtil
{
    /// <summary>
    /// Returns correctly styled input background colour & foreground colour, given the cell's formatting.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static string GetStyledInput(IReadOnlyCell cell)
    {
        var sb = new StyleBuilder();
        if (cell?.Format?.BackgroundColor != null)
            sb.AddStyle("background", cell.Format.BackgroundColor);
        else
            sb.AddStyle("background", "var(--sheet-bg-color)");

        if (cell?.Format?.ForegroundColor == null)
            sb.AddStyle("color", "var(--sheet-foreground-color)");
        else
            sb.AddStyle("color", cell.Format.ForegroundColor);

        return sb.ToString();
    }
}