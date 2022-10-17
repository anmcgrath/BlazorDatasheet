using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A column range. This range does not have an end position.
/// </summary>
public class ColumnRange : IRange
{
    public CellPosition Start { get; private set; }
    public int ColumnEnd { get; private set; }

    /// <summary>
    /// Create a column range that spans the start and end columns
    /// </summary>
    /// <param name="columnStart"></param>
    /// <param name="columnEnd"></param>
    public ColumnRange(int columnStart, int columnEnd)
    {
        // The start position is always at row = 0
        Start = new CellPosition(0, columnStart);
        ColumnEnd = columnEnd;
    }

    public bool Contains(int row, int col)
    {
        var c0 = Math.Min(Start.Col, ColumnEnd);
        var c1 = Math.Max(Start.Col, ColumnEnd);
        return col >= c0 && col <= c1;
    }

    public bool SpansCol(int col) => Contains(0, col);

    public bool SpansRow(int row) => true;

    public IRange Collapse()
    {
        return new Range(Start.Row, Start.Col);
    }

    public void Move(int dRow, int dCol, IFixedSizeRange? rangeLimit = null)
    {
        if (rangeLimit != null)
        {
            var newColStart = SheetMath.ClampInt(rangeLimit.Start.Col, rangeLimit.End.Col,
                                                 Start.Col + dCol);
            var newColEnd = SheetMath.ClampInt(rangeLimit.Start.Col, rangeLimit.End.Col,
                                               ColumnEnd);
            this.Start = new CellPosition(0, newColStart);
            this.ColumnEnd = newColEnd;
            return;
        }

        ColumnEnd += dCol;
        Start = new CellPosition(0, Start.Col + dCol);
        return;
    }

    public IRange Copy()
    {
        return new ColumnRange(Start.Col, ColumnEnd);
    }

    public IFixedSizeRange GetIntersection(IFixedSizeRange? range)
    {
        var columnFixed = new Range(-int.MaxValue, int.MaxValue, Start.Col, ColumnEnd);
        return columnFixed.GetIntersection(range);
    }

    public void ExtendTo(int row, int col, IFixedSizeRange? rangeLimit = null)
    {
        if (rangeLimit == null)
        {
            ColumnEnd = col;
            return;
        }

        var newColumnEnd = SheetMath
            .ClampInt(rangeLimit.Start.Col, rangeLimit.End.Col, col);

        ColumnEnd = col;
    }
}