using System.Collections;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Data;

/// <summary>
/// A range that has a specific start/stop position
/// </summary>
public class Range : IEnumerable<CellPosition>, IRange
{
    public CellPosition StartPosition { get; private set; }
    public CellPosition EndPosition { get; private set; }
    public int Height => Math.Abs(EndPosition.Row - StartPosition.Row) + 1;
    public int Width => Math.Abs(EndPosition.Row - StartPosition.Row) + 1;
    public int Area => Height * Width;

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
        StartPosition = new CellPosition(rowStart, colStart);
        EndPosition = new CellPosition(rowEnd, colEnd);
    }

    /// <summary>
    /// Determines whether a point is inside the range
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col)
    {
        var r0 = Math.Min(StartPosition.Row, EndPosition.Row);
        var r1 = Math.Max(StartPosition.Row, EndPosition.Row);
        var c0 = Math.Min(StartPosition.Col, EndPosition.Col);
        var c1 = Math.Max(StartPosition.Col, EndPosition.Col);
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
    public bool SpansCol(int col)
    {
        var c0 = Math.Min(StartPosition.Col, EndPosition.Col);
        var c1 = Math.Max(StartPosition.Col, EndPosition.Col);
        return col >= c0 && col <= c1;
    }

    /// <summary>
    /// Determines whether the row is spanned by the range
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool SpansRow(int row)
    {
        var r0 = Math.Min(StartPosition.Row, EndPosition.Row);
        var r1 = Math.Max(StartPosition.Row, EndPosition.Row);
        return row >= r0 &&
               row <= r1;
    }

    /// <summary>
    /// Updates the size of the range so that it is no larger than a range starting from (0, 0) with height/width =  (rows, cols)
    /// </summary>
    /// <param name="nRows"></param>
    /// <param name="nCols"></param>
    public void Constrain(int nRows, int nCols)
    {
        Constrain(0, nRows - 1, 0, nCols - 1);
    }

    public void Constrain(Range range)
    {
        Constrain(range.StartPosition.Row, range.EndPosition.Row, range.StartPosition.Col, range.EndPosition.Col);
    }

    public void Constrain(int otherRowStart, int otherRowEnd, int otherColStart, int otherColEnd)
    {
        var r0 = Constrain(otherRowStart, otherRowEnd, this.StartPosition.Row);
        var r1 = Constrain(otherRowStart, otherRowEnd, this.EndPosition.Row);
        var c0 = Constrain(otherColStart, otherColEnd, this.StartPosition.Col);
        var c1 = Constrain(otherColStart, otherColEnd, this.StartPosition.Col);
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
    public IRange Copy()
    {
        return new Range(StartPosition.Row, EndPosition.Row, StartPosition.Col, EndPosition.Col);
    }

    /// <summary>
    /// Returns a new copy of the range with the row, col starting point
    /// at the top left (minimum points).
    /// </summary>
    /// <returns></returns>
    public IRange CopyOrdered()
    {
        return new Range(
            Math.Min(StartPosition.Row, EndPosition.Row),
            Math.Max(StartPosition.Row, EndPosition.Row),
            Math.Min(StartPosition.Col, EndPosition.Col),
            Math.Max(StartPosition.Col, EndPosition.Col)
        );
    }

    public IEnumerator<CellPosition> GetEnumerator()
    {
        var r0 = StartPosition.Row;
        var r1 = EndPosition.Row;
        var c0 = StartPosition.Col;
        var c1 = EndPosition.Col;
        for (var row = r0; row <= r1; row++)
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