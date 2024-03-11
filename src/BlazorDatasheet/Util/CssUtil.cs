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
        var str = new StringBuilder();
        if (cell?.Format?.BackgroundColor == null)
            str.Append("background:var(--sheet-bg-color);");
        else
            str.Append($"background:{cell.Format.BackgroundColor};");

        if (cell?.Format?.ForegroundColor == null)
            str.Append("color:var(--sheet-foreground-color)");
        else
            str.Append($"color:{cell.Format.ForegroundColor};");

        return str.ToString();
    }

    /// <summary>
    /// Returns the css strings for producing width & max width of a cell given its location and span
    /// </summary>
    /// <param name="col"></param>
    /// <param name="colSpan"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static string GetCellWidthStyles(int col, int colSpan, CellLayoutProvider provider)
    {
        var width = provider.ComputeWidth(col, colSpan);
        var str = new StringBuilder();
        str.Append($"width:{width}px;");
        str.Append($"max-width:{width}px;");
        str.Append($"min-width:{width}px;");
        return str.ToString();
    }
}