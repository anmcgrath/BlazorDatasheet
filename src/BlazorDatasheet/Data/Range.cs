using System.Collections;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A range that has a specific start/end position
/// </summary>
public class Range : IFixedSizeRange
{
    public CellPosition StartPosition { get; private set; }
    public CellPosition EndPosition { get; private set; }
    public int Height => Math.Abs(EndPosition.Row - StartPosition.Row) + 1;
    public int Width => Math.Abs(EndPosition.Col - StartPosition.Col) + 1;
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

    public IRange Collapse()
    {
        return new Range(StartPosition.Row, StartPosition.Col);
    }

    public void Move(int dRow, int dCol, IFixedSizeRange? limitingRange = null)
    {
        this.StartPosition = new CellPosition(StartPosition.Row + dRow, StartPosition.Col + dCol);
        this.EndPosition = new CellPosition(EndPosition.Row + dRow, EndPosition.Col + dCol);
        if (limitingRange != null)
            this.Constrain(limitingRange);
    }

    /// <summary>
    /// Updates range so that it falls inside the range
    /// </summary>
    /// <param name="range"></param>
    public void Constrain(IFixedSizeRange? range)
    {
        Constrain(range.StartPosition.Row, range.EndPosition.Row, range.StartPosition.Col, range.EndPosition.Col);
    }

    /// <summary>
    /// Updates the range so that i falls inside the limits
    /// </summary>
    /// <param name="otherRowStart"></param>
    /// <param name="otherRowEnd"></param>
    /// <param name="otherColStart"></param>
    /// <param name="otherColEnd"></param>
    public void Constrain(int otherRowStart, int otherRowEnd, int otherColStart, int otherColEnd)
    {
        var r0 = SheetMath.ClampInt(otherRowStart, otherRowEnd, this.StartPosition.Row);
        var r1 = SheetMath.ClampInt(otherRowStart, otherRowEnd, this.EndPosition.Row);
        var c0 = SheetMath.ClampInt(otherColStart, otherColEnd, this.StartPosition.Col);
        var c1 = SheetMath.ClampInt(otherColStart, otherColEnd, this.EndPosition.Col);
        this.StartPosition = new CellPosition(r0, c0);
        this.EndPosition = new CellPosition(r1, c1);
    }

    /// <summary>
    /// Returns a new copy of the range.
    /// </summary>
    /// <returns></returns>
    public IRange Copy()
    {
        return new Range(StartPosition.Row, EndPosition.Row, StartPosition.Col, EndPosition.Col);
    }

    public IFixedSizeRange GetIntersection(IFixedSizeRange? range)
    {
        var thisRange = this.CopyOrdered();
        var otherRange = range.CopyOrdered();
        var x1 = thisRange.EndPosition.Col;
        var x2 = otherRange.EndPosition.Col;
        var y1 = thisRange.EndPosition.Row;
        var y2 = otherRange.EndPosition.Row;

        var xL = Math.Max(thisRange.StartPosition.Col, otherRange.StartPosition.Col);
        var xR = Math.Min(x1, x2);
        if (xR < xL)
            return null;
        else
        {
            var yB = Math.Min(y1, y2);
            var yT = Math.Max(thisRange.StartPosition.Row, otherRange.StartPosition.Row);
            if (yB < yT)
                return null;
            else
                return new Range(yT, yB, xL, xR);
        }
    }

    public void ExtendTo(int row, int col, IFixedSizeRange? rangeLimit = null)
    {
        EndPosition = new CellPosition(row, col);
        if (rangeLimit != null)
            this.Constrain(rangeLimit);
    }

    /// <summary>
    /// Returns a new copy of the range with the row, col starting point
    /// at the top left (minimum points).
    /// </summary>
    /// <returns></returns>
    public IFixedSizeRange CopyOrdered()
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
        var rowDir = EndPosition.Row >= StartPosition.Row ? 1 : -1;
        var colDir = EndPosition.Col >= StartPosition.Col ? 1 : -1;
        var row = StartPosition.Row;
        var col = StartPosition.Col;

        for (var i = 0; i < Height; i++)
        {
            // Reset column at start of each row
            col = StartPosition.Col;

            for (var j = 0; j < Width; j++)
            {
                yield return new CellPosition(row, col);
                col += colDir;
            }

            row += rowDir;
        }
    }

    public override string ToString()
    {
        return $"Range from ({StartPosition.Row}, {StartPosition.Col}) to ({EndPosition.Row}, {EndPosition.Col})";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj is IFixedSizeRange fr)
            return fr.StartPosition.Row == StartPosition.Row
                   && fr.StartPosition.Col == StartPosition.Col
                   && fr.EndPosition.Row == EndPosition.Row
                   && fr.EndPosition.Col == EndPosition.Col;

        return false;
    }
}