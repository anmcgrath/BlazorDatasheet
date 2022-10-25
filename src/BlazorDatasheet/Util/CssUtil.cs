using System.Text;
using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Util;

public class CssUtil
{
    /// <summary>
    /// Returns correctly styled input background colour & foreground colour, given the cell's formatting.
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public static string GetStyledInput(IReadOnlyCell cell)
    {
        var str = new StringBuilder();
        if (cell == null || cell.Formatting == null)
        {
            str.Append("background:var(--sheet-bg-color);");
            str.Append("color:var(--sheet-foreground-color)");
        }
        else if (cell.Formatting != null)
        {
            str.Append($"background:{cell.Formatting.BackgroundColor};");
            str.Append($"color:{cell.Formatting.ForegroundColor};");
        }
        return str.ToString();
    }
}