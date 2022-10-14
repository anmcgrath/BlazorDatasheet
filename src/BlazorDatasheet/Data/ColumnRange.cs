using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A column range. This range does not have an end position.
/// </summary>
public class ColumnRange : IRange
{
    public CellPosition StartPosition { get; private set; }
    public int ColumnEnd { get; private set; }

    /// <summary>
    /// Create a column range that spans the start and end columns
    /// </summary>
    /// <param name="columnStart"></param>
    /// <param name="columnEnd"></param>
    public ColumnRange(int columnStart, int columnEnd)
    {
        // The start position is always at row = 0
        StartPosition = new CellPosition(0, columnStart);
        ColumnEnd = columnEnd;
    }

    public bool Contains(int row, int col)
    {
        var c0 = Math.Min(StartPosition.Col, ColumnEnd);
        var c1 = Math.Max(StartPosition.Col, ColumnEnd);
        return col >= c0 && col <= c1;
    }

    public bool SpansCol(int col) => Contains(0, col);

    public bool SpansRow(int row) => true;

    public IRange Collapse()
    {
        return new Range(StartPosition.Row, StartPosition.Col);
    }

    public void Move(int dRow, int dCol, IFixedSizeRange? rangeLimit = null)
    {
        if (rangeLimit != null)
        {
            var newColStart = SheetMath.ClampInt(rangeLimit.StartPosition.Col, rangeLimit.EndPosition.Col,
                                                 StartPosition.Col + dCol);
            var newColEnd = SheetMath.ClampInt(rangeLimit.StartPosition.Col, rangeLimit.EndPosition.Col,
                                               ColumnEnd);
            this.StartPosition = new CellPosition(0, newColStart);
            this.ColumnEnd = newColEnd;
            return;
        }

        ColumnEnd += dCol;
        StartPosition = new CellPosition(0, StartPosition.Col + dCol);
        return;
    }

    public IRange Copy()
    {
        return new ColumnRange(StartPosition.Col, ColumnEnd);
    }

    public IFixedSizeRange GetIntersection(IFixedSizeRange? range)
    {
        var columnFixed = new Range(0, int.MaxValue, StartPosition.Col, ColumnEnd);
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
            .ClampInt(rangeLimit.StartPosition.Col, rangeLimit.EndPosition.Col, col);

        ColumnEnd = col;
    }
}