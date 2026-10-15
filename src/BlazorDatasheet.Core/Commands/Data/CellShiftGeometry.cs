using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// The regions involved in inserting or removing cells at a region, shifting along an axis.
/// </summary>
internal readonly record struct CellShiftGeometry(IRegion Region, IRegion Band, IRegion HalfBand, int Count)
{
    /// <summary>
    /// The geometry of a shift at <paramref name="region"/> clipped to the sheet, or null when the region lies
    /// outside the sheet. The half-band is clipped to the sheet too.
    /// </summary>
    public static CellShiftGeometry? Create(Sheet sheet, IRegion region, Axis axis)
    {
        var clipped = region.GetIntersection(sheet.Region);
        if (clipped == null)
            return null;

        IRegion band = axis == Axis.Col
            ? new RowRegion(clipped.Top, clipped.Bottom)
            : new ColumnRegion(clipped.Left, clipped.Right);
        var halfBand = CellStore.GetHalfBand(clipped, axis).GetIntersection(sheet.Region)!;
        var count = axis == Axis.Col ? clipped.Width : clipped.Height;
        return new CellShiftGeometry(clipped, band, halfBand, count);
    }
}
