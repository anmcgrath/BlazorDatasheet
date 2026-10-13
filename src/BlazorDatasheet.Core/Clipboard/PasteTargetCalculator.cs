using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Clipboard;

public static class PasteTargetCalculator
{
    /// <summary>
    /// Where the top-left of the source block lands when pasted into <paramref name="selection"/>. A selection
    /// that is an exact multiple of the source size is tiled, as in Excel; otherwise the single origin is the
    /// selection's top-left.
    /// </summary>
    public static IReadOnlyList<CellPosition> GetTileOrigins(IRegion source, IRegion selection, bool allowTiling)
    {
        var topLeft = selection.TopLeft;
        if (!allowTiling || selection.IsSingleCell() ||
            selection.Height % source.Height != 0 || selection.Width % source.Width != 0)
            return new[] { topLeft };

        var origins = new List<CellPosition>();
        for (var row = selection.Top; row <= selection.Bottom; row += source.Height)
        for (var col = selection.Left; col <= selection.Right; col += source.Width)
            origins.Add(new CellPosition(row, col));
        return origins;
    }

    /// <summary>
    /// The region a block the size of <paramref name="source"/> covers when its top-left is at
    /// <paramref name="origin"/>.
    /// </summary>
    public static Region GetBlockAt(IRegion source, CellPosition origin)
    {
        return new Region(origin.row, origin.row + source.Height - 1, origin.col, origin.col + source.Width - 1);
    }
}
