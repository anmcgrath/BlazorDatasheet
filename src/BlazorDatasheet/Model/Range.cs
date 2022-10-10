using System.Collections;

namespace BlazorDatasheet.Model;

public class Range : IEnumerable<CellPosition>
{
    public int RowStart { get; set; }
    public int ColStart { get; set; }
    public int RowEnd { get; set; }
    public int ColEnd { get; set; }

    /// <summary>
    /// A single (width/height = 1) range with position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public Range(int row, int col) : this(row, row, col, col)
    {
    }

    /// <summary>
    /// A rectangular range specified by start/end rows & cols
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    public Range(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        RowStart = rowStart;
        RowEnd = rowEnd;
        ColStart = colStart;
        ColEnd = colEnd;
    }

    /// <summary>
    /// Determines whether a point is inside the range
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col)
    {
        var r0 = Math.Min(RowStart, RowEnd);
        var r1 = Math.Max(RowStart, RowEnd);
        var c0 = Math.Min(ColStart, ColEnd);
        var c1 = Math.Max(ColStart, ColEnd);
        return row >= r0 &&
               row <= r1 &&
               col >= c0 &&
               col <= c1;
    }

    /// <summary>
    /// Determines whether the column is spanned by the range
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool ContainsCol(int col)
    {
        var c0 = Math.Min(ColStart, ColEnd);
        var c1 = Math.Max(ColStart, ColEnd);
        return col >= c0 && col <= c1;
    }

    /// <summary>
    /// Determines whether the row is spanned by the range
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool ContainsRow(int row)
    {
        var r0 = Math.Min(RowStart, RowEnd);
        var r1 = Math.Max(RowStart, RowEnd);
        return row >= r0 &&
               row <= r1;
    }

    /// <summary>
    /// Updates the size of the range so that it is no larger than a range starting from (0, 0) to (rows, cols)
    /// </summary>
    /// <param name="rows"></param>
    /// <param name="cols"></param>
    public void Constrain(int rows, int cols)
    {
        Constrain(0, rows - 1, 0, cols - 1);
    }

    public void Constrain(Range range)
    {
        Constrain(range.RowStart, range.RowEnd, range.ColStart, range.ColEnd);
    }

    public void Constrain(int otherRowStart, int otherRowEnd, int otherColStart, int otherColEnd)
    {
        RowStart = Constrain(otherRowStart, otherRowEnd, this.RowStart);
        RowEnd = Constrain(otherRowStart, otherRowEnd, this.RowEnd);
        ColStart = Constrain(otherColStart, otherColEnd, this.ColStart);
        ColEnd = Constrain(otherColStart, otherColEnd, this.ColEnd);
    }

    /// <summary>
    /// Constrains a single value to be inside max/min (aka clamp)
    /// </summary>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <param name="val"></param>
    /// <returns></returns>
    private int Constrain(int min, int max, int val)
    {
        if (val < min)
            return min;
        if (val > max)
            return max;
        return val;
    }

    /// <summary>
    /// Returns a new copy of the range.
    /// </summary>
    /// <returns></returns>
    public Range Copy()
    {
        return new Range(RowStart, RowEnd, ColStart, ColEnd);
    }

    /// <summary>
    /// Returns a new copy of the range with the row, col starting point
    /// at the top left (minimum points).
    /// </summary>
    /// <returns></returns>
    public Range CopyOrdered()
    {
        return new Range(
            Math.Min(RowStart, RowEnd),
            Math.Max(RowStart, RowEnd),
            Math.Min(ColStart, ColEnd),
            Math.Max(ColStart, ColEnd)
        );
    }

    public IEnumerator<CellPosition> GetEnumerator()
    {
        for (var row = RowStart; row <= RowEnd; row++)
        {
            for (var col = ColStart; col <= ColEnd; col++)
            {
                yield return new CellPosition(row, col);
            }
        }
    }

    public override string ToString()
    {
        return $"Range from ({RowStart}, {ColStart}) to ({RowEnd}, {ColEnd})";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}