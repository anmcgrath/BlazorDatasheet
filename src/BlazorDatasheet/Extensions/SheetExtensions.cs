using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Extensions;

public static class SheetExtensions
{
    public static IRegion GetFrozenTopRegion(this Sheet sheet)
    {
        if (sheet.FreezeState.Top <= 0)
            return new EmptyRegion();

        var bottom = Math.Clamp(sheet.FreezeState.Top - 1, 0, sheet.NumRows - 1);
        return new Region(0, bottom, 0, sheet.NumCols - 1);
    }

    public static IRegion GetFrozenBottomRegion(this Sheet sheet)
    {
        if (sheet.FreezeState.Bottom <= 0)
            return new EmptyRegion();

        var top = Math.Clamp(sheet.NumRows - sheet.FreezeState.Bottom, 0, sheet.NumRows - 1);
        return new Region(top, sheet.NumRows - 1, 0, sheet.NumCols - 1);
    }

    public static IRegion GetFrozenLeftRegion(this Sheet sheet)
    {
        if (sheet.FreezeState.Left <= 0)
            return new EmptyRegion();

        var right = Math.Clamp(sheet.FreezeState.Left - 1, 0, sheet.NumCols - 1);
        return new Region(0, sheet.NumRows - 1, 0, right);
    }

    public static IRegion GetFrozenRightRegion(this Sheet sheet)
    {
        if (sheet.FreezeState.Right <= 0)
            return new EmptyRegion();

        var left = Math.Clamp(sheet.NumCols - sheet.FreezeState.Right, 0, sheet.NumCols - 1);
        return new Region(0, sheet.NumRows - 1, left, sheet.NumCols - 1);
    }
}