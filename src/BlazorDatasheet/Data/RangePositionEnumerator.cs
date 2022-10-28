using System.Collections;

namespace BlazorDatasheet.Data;

internal class RangePositionEnumerator : IEnumerable<(int row, int col)>
{
    private readonly BRange _range;

    internal RangePositionEnumerator(BRange range)
    {
        _range = range;
    }

    public IEnumerator<(int row, int col)> GetEnumerator()
    {
        foreach (var region in _range.Regions)
        {
            var fixedRegion = _range.Sheet.Region.GetIntersection(region);
            var row = fixedRegion.TopLeft.Row;
            var col = fixedRegion.TopLeft.Col;
            var w = fixedRegion.Width;
            var h = fixedRegion.Height;

            for (var i = 0; i < h; i++)
            {
                // Reset column at start of each row
                col = fixedRegion.TopLeft.Col;
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