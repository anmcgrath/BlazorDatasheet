using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Edit;

public enum ReferenceDragMode
{
    /// <summary>
    /// The reference keeps its size and follows the pointer.
    /// </summary>
    Move,

    /// <summary>
    /// One corner of the reference follows the pointer, and the opposite corner stays where it is.
    /// </summary>
    Resize
}

/// <summary>
/// Calculates the region that a reference in a formula refers to, as it is dragged over the sheet.
/// Both calculations start from the region as it was when the drag began, so that a drag that comes back
/// to where it started gives back the region it started with.
/// </summary>
public static class ReferenceDragCalculator
{
    /// <summary>
    /// Moves <paramref name="original"/> by the distance from <paramref name="grab"/>, which is the cell that was
    /// under the pointer when the drag began, to <paramref name="pointer"/>. The region keeps its size and stays
    /// inside <paramref name="bounds"/>. Column and row regions only move along their axis.
    /// </summary>
    public static IRegion Move(IRegion original, CellPosition grab, CellPosition pointer, IRegion bounds)
    {
        pointer = Clamp(pointer, bounds);

        if (original is ColumnRegion)
        {
            var offset = ClampOffset(pointer.col - grab.col, original.Left, original.Right, bounds.Left,
                bounds.Right);
            return new ColumnRegion(original.Left + offset, original.Right + offset);
        }

        if (original is RowRegion)
        {
            var offset = ClampOffset(pointer.row - grab.row, original.Top, original.Bottom, bounds.Top,
                bounds.Bottom);
            return new RowRegion(original.Top + offset, original.Bottom + offset);
        }

        var dRow = ClampOffset(pointer.row - grab.row, original.Top, original.Bottom, bounds.Top, bounds.Bottom);
        var dCol = ClampOffset(pointer.col - grab.col, original.Left, original.Right, bounds.Left, bounds.Right);
        return new Region(original.Top + dRow, original.Bottom + dRow, original.Left + dCol, original.Right + dCol);
    }

    /// <summary>
    /// Stretches <paramref name="original"/> between <paramref name="anchor"/>, which is the corner opposite the one
    /// being dragged, and <paramref name="pointer"/>. Column and row regions only resize along their axis.
    /// </summary>
    public static IRegion Resize(IRegion original, CellPosition anchor, CellPosition pointer, IRegion bounds)
    {
        pointer = Clamp(pointer, bounds);

        if (original is ColumnRegion)
            return new ColumnRegion(Math.Min(anchor.col, pointer.col), Math.Max(anchor.col, pointer.col));

        if (original is RowRegion)
            return new RowRegion(Math.Min(anchor.row, pointer.row), Math.Max(anchor.row, pointer.row));

        return new Region(
            Math.Min(anchor.row, pointer.row), Math.Max(anchor.row, pointer.row),
            Math.Min(anchor.col, pointer.col), Math.Max(anchor.col, pointer.col));
    }

    // the headings report a row or column of -1
    private static CellPosition Clamp(CellPosition position, IRegion bounds) =>
        new(Math.Clamp(position.row, bounds.Top, bounds.Bottom),
            Math.Clamp(position.col, bounds.Left, bounds.Right));

    private static int ClampOffset(int offset, int start, int end, int min, int max)
    {
        var lowest = min - start;
        var highest = max - end;
        // a region that doesn't fit inside the bounds has nowhere to move to
        return lowest > highest ? 0 : Math.Clamp(offset, lowest, highest);
    }
}
