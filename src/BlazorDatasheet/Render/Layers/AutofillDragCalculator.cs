using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;

namespace BlazorDatasheet.Render.Layers;

/// <summary>
/// Works out the region an autofill drag would fill, from the pointer position in sheet coordinates.
/// Held apart from <see cref="AutofillLayer"/> so the geometry can be exercised without a rendered
/// component or a browser.
/// </summary>
internal static class AutofillDragCalculator
{
    /// <summary>
    /// How far the pointer has to travel from the fill handle before the drag counts as a drag.
    /// </summary>
    internal const double DragThresholdInpx = 5;

    /// <summary>
    /// Calculates the region that would be filled if the drag ended with the pointer at
    /// (<paramref name="sheetX"/>, <paramref name="sheetY"/>).
    /// </summary>
    /// <param name="sheetX">Pointer x, in sheet coordinates.</param>
    /// <param name="sheetY">Pointer y, in sheet coordinates.</param>
    /// <returns>
    /// False when the pointer is still inside the dead zone around the fill handle, in which case the
    /// caller should leave whatever preview it is already showing alone.
    /// </returns>
    public static bool TryCalculatePreviewRegion(
        Sheet sheet,
        IRegion activeRegion,
        CellLayoutProvider layoutProvider,
        double sheetX,
        double sheetY,
        out IRegion? previewRegion)
    {
        previewRegion = null;

        // the fill handle sits on the bottom right corner of the selection, so the drag is measured
        // from there.
        var selRect = activeRegion.GetRect(sheet);
        var dx = sheetX - (selRect.X + selRect.Width);
        var dy = sheetY - (selRect.Y + selRect.Height);

        if (Math.Abs(dx) < DragThresholdInpx && Math.Abs(dy) < DragThresholdInpx)
            return false;

        var cellAtMouse = layoutProvider.ComputeCell(sheetX, sheetY);

        var region = activeRegion.Contains(cellAtMouse)
            ? CalculateContractRegion(activeRegion, dx, dy, cellAtMouse)
            : CalculateExpandRegion(activeRegion, cellAtMouse);

        previewRegion = sheet.Region.GetIntersection(region);
        return true;
    }

    private static IRegion CalculateContractRegion(IRegion activeRegion, double dx, double dy,
        CellPosition cellMousePosition)
    {
        var left = activeRegion.Left;
        var top = activeRegion.Top;
        var axis = Math.Abs(dx) >= Math.Abs(dy) ? Axis.Col : Axis.Row;
        var right = axis == Axis.Col ? cellMousePosition.col : activeRegion.Right;
        var bottom = axis == Axis.Row ? cellMousePosition.row : activeRegion.Bottom;
        return new Region(top, bottom, left, right);
    }

    private static IRegion CalculateExpandRegion(IRegion activeRegion, CellPosition cellMousePosition)
    {
        var axis = GetExpansionAxis(activeRegion, cellMousePosition);

        var expandTo = axis == Axis.Col
            ? new Region(activeRegion.Bottom, cellMousePosition.col)
            : new Region(cellMousePosition.row, activeRegion.Left);

        return activeRegion.GetBoundingRegion(expandTo);
    }

    private static Axis GetExpansionAxis(IRegion activeRegion, CellPosition cellMousePosition)
    {
        var containsX = cellMousePosition.col >= activeRegion.Left && cellMousePosition.col <= activeRegion.Right;
        var containsY = cellMousePosition.row >= activeRegion.Top && cellMousePosition.row <= activeRegion.Bottom;

        if (containsY && !containsX)
            return Axis.Col;

        if (containsX && !containsY)
            return Axis.Row;

        var dx = cellMousePosition.col < activeRegion.Left
            ? activeRegion.Left - cellMousePosition.col
            : cellMousePosition.col - activeRegion.Right;

        var dy = cellMousePosition.row < activeRegion.Top
            ? activeRegion.Top - cellMousePosition.row
            : cellMousePosition.row - activeRegion.Bottom;

        return Math.Abs(dx) >= Math.Abs(dy) ? Axis.Col : Axis.Row;
    }
}