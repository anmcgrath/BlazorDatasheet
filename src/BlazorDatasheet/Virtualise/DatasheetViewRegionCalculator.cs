using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Virtualise;

internal static class DatasheetViewRegionCalculator
{
    public static Region GetConstrainedViewRegion(Region? viewRegion, int numRows, int numCols)
    {
        if (numRows <= 0 || numCols <= 0)
            return new Region(0, 0, 0, 0);

        var sheetRegion = new Region(0, numRows - 1, 0, numCols - 1);
        if (viewRegion == null)
            return sheetRegion;

        return viewRegion.GetIntersection(sheetRegion) as Region ?? sheetRegion;
    }

    public static Region GetMainViewRegion(Region constrainedViewRegion, int numRows, int numCols,
        int frozenTopCount, int frozenBottomCount, int frozenLeftCount, int frozenRightCount)
    {
        if (numRows <= 0 || numCols <= 0)
            return new Region(0, 0, 0, 0);

        var topFreeze = Math.Clamp(frozenTopCount, 0, numRows);
        var bottomFreeze = Math.Clamp(frozenBottomCount, 0, numRows);
        var leftFreeze = Math.Clamp(frozenLeftCount, 0, numCols);
        var rightFreeze = Math.Clamp(frozenRightCount, 0, numCols);

        var mainTop = Math.Max(topFreeze, constrainedViewRegion.Top);
        var mainBottom = Math.Min(numRows - bottomFreeze - 1, constrainedViewRegion.Bottom);
        var mainLeft = Math.Max(leftFreeze, constrainedViewRegion.Left);
        var mainRight = Math.Min(numCols - rightFreeze - 1, constrainedViewRegion.Right);

        mainTop = Math.Clamp(mainTop, constrainedViewRegion.Top, constrainedViewRegion.Bottom);
        mainBottom = Math.Clamp(mainBottom, mainTop, constrainedViewRegion.Bottom);
        mainLeft = Math.Clamp(mainLeft, constrainedViewRegion.Left, constrainedViewRegion.Right);
        mainRight = Math.Clamp(mainRight, mainLeft, constrainedViewRegion.Right);

        return new Region(mainTop, mainBottom, mainLeft, mainRight);
    }

    public static Region GetFrozenTopRegion(Region constrainedViewRegion, int numRows, int frozenTopCount)
    {
        if (numRows <= 0)
            return new Region(0, 0, constrainedViewRegion.Left, constrainedViewRegion.Right);

        var topFreeze = Math.Clamp(frozenTopCount, 0, numRows);
        var bottom = Math.Clamp(topFreeze - 1, 0, numRows - 1);
        return new Region(0, bottom, constrainedViewRegion.Left, constrainedViewRegion.Right);
    }

    public static Region GetFrozenBottomRegion(Region constrainedViewRegion, int numRows, int frozenBottomCount)
    {
        if (numRows <= 0)
            return new Region(0, 0, constrainedViewRegion.Left, constrainedViewRegion.Right);

        var bottomFreeze = Math.Clamp(frozenBottomCount, 0, numRows);
        var top = Math.Clamp(numRows - bottomFreeze, 0, numRows - 1);
        return new Region(top, numRows - 1, constrainedViewRegion.Left, constrainedViewRegion.Right);
    }

    public static Region GetFrozenLeftRegion(Region mainViewRegion, int numCols, int frozenLeftCount)
    {
        if (numCols <= 0)
            return new Region(mainViewRegion.Top, mainViewRegion.Bottom, 0, 0);

        var leftFreeze = Math.Clamp(frozenLeftCount, 0, numCols);
        var right = Math.Clamp(leftFreeze - 1, 0, numCols - 1);
        return new Region(mainViewRegion.Top, mainViewRegion.Bottom, 0, right);
    }

    public static Region GetFrozenRightRegion(Region mainViewRegion, int numCols, int frozenRightCount)
    {
        if (numCols <= 0)
            return new Region(mainViewRegion.Top, mainViewRegion.Bottom, 0, 0);

        var rightFreeze = Math.Clamp(frozenRightCount, 0, numCols);
        var left = Math.Clamp(numCols - rightFreeze, 0, numCols - 1);
        return new Region(mainViewRegion.Top, mainViewRegion.Bottom, left, numCols - 1);
    }
}
