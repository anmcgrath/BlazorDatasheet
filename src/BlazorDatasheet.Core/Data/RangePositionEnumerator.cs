using System.Collections;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

internal class RangePositionEnumerator : IEnumerable<CellPosition>
{
    private readonly BRange _range;

    internal RangePositionEnumerator(BRange range)
    {
        _range = range;
    }

    public IEnumerator<CellPosition> GetEnumerator()
    {
        foreach (var region in _range.Regions)
        {
            var fixedRegion = _range.Sheet.Region.GetIntersection(region);
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
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}