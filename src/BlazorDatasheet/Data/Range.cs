using System.Collections;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A range that has a specific start/end position
/// </summary>
public class Range : IFixedSizeRange
{
    public CellPosition Start { get; private set; }
    public CellPosition End { get; private set; }
    public int Height => Math.Abs(End.Row - Start.Row) + 1;
    public int Width => Math.Abs(End.Col - Start.Col) + 1;
    public int RowDir => End.Row >= Start.Row ? 1 : -1;
    public int ColDir => End.Col >= Start.Col ? 1 : -1;
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
        Start = new CellPosition(rowStart, colStart);
        End = new CellPosition(rowEnd, colEnd);
    }

    /// <summary>
    /// Determines whether a point is inside the range
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col)
    {
        var r0 = Math.Min(Start.Row, End.Row);
        var r1 = Math.Max(Start.Row, End.Row);
        var c0 = Math.Min(Start.Col, End.Col);
        var c1 = Math.Max(Start.Col, End.Col);
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
        var c0 = Math.Min(Start.Col, End.Col);
        var c1 = Math.Max(Start.Col, End.Col);
        return col >= c0 && col <= c1;
    }

    /// <summary>
    /// Determines whether the row is spanned by the range
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool SpansRow(int row)
    {
        var r0 = Math.Min(Start.Row, End.Row);
        var r1 = Math.Max(Start.Row, End.Row);
        return row >= r0 &&
               row <= r1;
    }

    public IRange Collapse()
    {
        return new Range(Start.Row, Start.Col);
    }

    public void Move(int dRow, int dCol, IFixedSizeRange? limitingRange = null)
    {
        this.Start = new CellPosition(Start.Row + dRow, Start.Col + dCol);
        this.End = new CellPosition(End.Row + dRow, End.Col + dCol);
        if (limitingRange != null)
            this.Constrain(limitingRange);
    }

    /// <summary>
    /// Updates range so that it falls inside the range
    /// </summary>
    /// <param name="range"></param>
    public void Constrain(IFixedSizeRange? range)
    {
        Constrain(range.Start.Row, range.End.Row, range.Start.Col, range.End.Col);
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
        var r0 = SheetMath.ClampInt(otherRowStart, otherRowEnd, this.Start.Row);
        var r1 = SheetMath.ClampInt(otherRowStart, otherRowEnd, this.End.Row);
        var c0 = SheetMath.ClampInt(otherColStart, otherColEnd, this.Start.Col);
        var c1 = SheetMath.ClampInt(otherColStart, otherColEnd, this.End.Col);
        this.Start = new CellPosition(r0, c0);
        this.End = new CellPosition(r1, c1);
    }

    /// <summary>
    /// Returns a new copy of the range.
    /// </summary>
    /// <returns></returns>
    public IRange Copy()
    {
        return new Range(Start.Row, End.Row, Start.Col, End.Col);
    }

    public IFixedSizeRange GetIntersection(IFixedSizeRange? range)
    {
        var thisRange = this.CopyOrdered();
        var otherRange = range.CopyOrdered();
        var x1 = thisRange.End.Col;
        var x2 = otherRange.End.Col;
        var y1 = thisRange.End.Row;
        var y2 = otherRange.End.Row;

        IFixedSizeRange? intersection = null;

        var xL = Math.Max(thisRange.Start.Col, otherRange.Start.Col);
        var xR = Math.Min(x1, x2);
        if (xR < xL)
            intersection = null;
        else
        {
            var yB = Math.Min(y1, y2);
            var yT = Math.Max(thisRange.Start.Row, otherRange.Start.Row);
            if (yB < yT)
                intersection = null;
            else
                intersection = new Range(yT, yB, xL, xR);
        }

        if (intersection != null)
            intersection.SetOrder(this.RowDir, this.ColDir);

        return intersection;
    }

    public void ExtendTo(int row, int col, IFixedSizeRange? rangeLimit = null)
    {
        End = new CellPosition(row, col);
        if (rangeLimit != null)
            this.Constrain(rangeLimit);
    }

    public List<IFixedSizeRange> Break(CellPosition position, bool preserveOrder = false)
    {
        return Break(new Range(position.Row, position.Col), preserveOrder);
    }

    public List<IFixedSizeRange> Break(IFixedSizeRange inputRange, bool preserveOrder = false)
    {
        var range = inputRange.CopyOrdered().GetIntersection(this);
        if (range == null)
            return new List<IFixedSizeRange>() { this.Copy() as IFixedSizeRange };

        var thisOrdered = this.CopyOrdered();

        // The range can split into at most 4 ranges
        // We attempt to create all the ranges and don't use any that are empty
        // Start from top to bottom. For example, consider the below case where we
        // break the larger range by the cell shown as \\
        // This is a case where we end up with 4 ranges shown as 1, 2, 3, 4

        // | 1  |  1 |  1 |
        // | 2  | \\ |  3 |
        // | 4  |  4 |  4 |

        // the case below shows ,for example, when we would end up with only range 3 and 4

        // | \\ | \\ |  3 |
        // | \\ | \\ |  3 |
        // | 4  |  4 |  4 |

        var r1IsEmpty = thisOrdered.Start.Row == range.Start.Row;
        var r2IsEmpty = thisOrdered.Start.Col == range.Start.Col;
        var r3IsEmpty = thisOrdered.End.Col == range.End.Col;
        var r4IsEmpty = thisOrdered.End.Row == range.End.Row;

        var ranges = new List<IFixedSizeRange>();

        if (!r1IsEmpty)
        {
            var r1 = new Range(thisOrdered.Start.Row, range.Start.Row - 1,
                               thisOrdered.Start.Col,
                               thisOrdered.End.Col);
            ranges.Add(r1);
        }

        if (!r2IsEmpty)
        {
            var r2 = new Range(range.Start.Row, range.End.Row, thisOrdered.Start.Col,
                               range.Start.Col - 1);
            ranges.Add(r2);
        }

        if (!r3IsEmpty)
        {
            var r3 = new Range(range.Start.Row, range.End.Row, range.Start.Col + 1,
                               thisOrdered.End.Col);
            ranges.Add(r3);
        }

        if (!r4IsEmpty)
        {
            var r4 = new Range(range.End.Row + 1, thisOrdered.End.Row, thisOrdered.Start.Col,
                               thisOrdered.End.Col);
            ranges.Add(r4);
        }

        if (preserveOrder)
        {
            foreach (var newRange in ranges)
                newRange.SetOrder(this.RowDir, this.ColDir);
        }

        return ranges;
    }

    /// <summary>
    /// Change start/end positions so that the range is ordered in the direction specified
    /// </summary>
    /// <param name="rowDir"></param>
    /// <param name="colDir"></param>
    public void SetOrder(int rowDir, int colDir)
    {
        var r0 = Math.Min(Start.Row, End.Row);
        var r1 = Math.Max(Start.Row, End.Row);
        var c0 = Math.Min(Start.Col, End.Col);
        var c1 = Math.Max(Start.Col, End.Col);
        
        Start = new CellPosition(rowDir == 1 ? r0 : r1, colDir == 1 ? c0 : c1);
        End = new CellPosition(rowDir == 1 ? r1 : r0, colDir == 1 ? c1 : c0);
    }

    /// <summary>
    /// Returns a new copy of the range with the row, col starting point
    /// at the top left (minimum points).
    /// </summary>
    /// <returns></returns>
    public IFixedSizeRange CopyOrdered()
    {
        return new Range(
            Math.Min(Start.Row, End.Row),
            Math.Max(Start.Row, End.Row),
            Math.Min(Start.Col, End.Col),
            Math.Max(Start.Col, End.Col)
        );
    }

    public IEnumerator<CellPosition> GetEnumerator()
    {
        var rowDir = End.Row >= Start.Row ? 1 : -1;
        var colDir = End.Col >= Start.Col ? 1 : -1;
        var row = Start.Row;
        var col = Start.Col;

        for (var i = 0; i < Height; i++)
        {
            // Reset column at start of each row
            col = Start.Col;

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
        return $"Range from ({Start.Row}, {Start.Col}) to ({End.Row}, {End.Col})";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public override bool Equals(object? obj)
    {
        if (obj is IFixedSizeRange fr)
            return fr.Start.Row == Start.Row
                   && fr.Start.Col == Start.Col
                   && fr.End.Row == End.Row
                   && fr.End.Col == End.Col;

        return false;
    }
}