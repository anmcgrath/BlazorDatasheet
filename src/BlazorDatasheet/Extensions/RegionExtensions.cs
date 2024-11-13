using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Extensions;

public static class RegionExtensions
{
    public static Rect GetRect(this IRegion region, Sheet sheet)
    {
        var left = sheet.Columns.GetVisualLeft(region.Left);
        var top = sheet.Rows.GetVisualTop(region.Top);
        var w = sheet.Columns.GetVisualWidthBetween(region.Left, region.Right + 1);
        var h = sheet.Rows.GetVisualHeightBetween(region.Top, region.Bottom + 1);
        return new Rect(left, top, w, h);
    }
}