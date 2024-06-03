using System.Collections;
using System.Diagnostics;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

internal class RangePositionEnumerator : IEnumerable<CellPosition>
{
    private readonly SheetRange _range;

    internal RangePositionEnumerator(SheetRange range)
    {
        _range = range;
    }

    public IEnumerator<CellPosition> GetEnumerator()
    {
        var fixedRegion = _range.Sheet.Region.GetIntersection(_range.Region);
        if(fixedRegion == null)
            yield break;
        
        var row = fixedRegion.TopLeft.row;
        var col = fixedRegion.TopLeft.col;
        var w = fixedRegion.Width;
        var h = fixedRegion.Height;

        for (var i = 0; i < h; i++)
        {
            // Reset column at start of each row
            col = fixedRegion.TopLeft.col;
            for (var j = 0; j < w; j++)
            {
                yield return new(row, col);
                col++;
            }

            row++;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}