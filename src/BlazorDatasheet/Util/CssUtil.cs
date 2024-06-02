using System.Text;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.Formula.Core;
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
        sb.AddStyleNotNull("background", cell?.Format?.BackgroundColor);
        sb.AddStyleNotNull("color", cell?.Format?.ForegroundColor);
        if (cell?.ValueType == CellValueType.Number && cell?.Format?.TextAlign == null && cell?.Formula == null)
            sb.AddStyle("text-align", "right");
        else if (cell?.Formula != null)
            sb.AddStyle("text-align", "left");
        else
            sb.AddStyleNotNull("text-align", cell?.Format?.TextAlign);
        
        sb.AddStyleNotNull("font-weight", cell?.Format?.FontWeight);
        return sb.ToString();
    }
}