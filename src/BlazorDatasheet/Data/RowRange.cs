using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A column range. This range does not have an end position.
/// </summary>
public class RowRange : IRange
{
    public CellPosition StartPosition { get; private set; }
    public int RowEnd { get; private set; }

    public RowRange(int rowStart, int rowEnd)
    {
        StartPosition = new CellPosition(rowStart, 0);
        RowEnd = rowEnd;
    }

    public bool Contains(int row, int col)
    {
        var r0 = Math.Min(StartPosition.Row, RowEnd);
        var r1 = Math.Max(StartPosition.Row, RowEnd);
        return row >= r0 && row <= r1;
    }

    public bool SpansCol(int col) => true;

    public bool SpansRow(int row) => Contains(row, 0);

    public IRange Collapse() => new Range(StartPosition.Row, StartPosition.Col);

    public void Move(int dRow, int dCol, IFixedSizeRange? rangeLimit = null)
    {
        if (rangeLimit != null)
        {
            var newRowStart = SheetMath.ClampInt(rangeLimit.StartPosition.Row, rangeLimit.EndPosition.Row,
                                                 StartPosition.Row + dRow);
            var newRowEnd = SheetMath.ClampInt(rangeLimit.StartPosition.Row, rangeLimit.EndPosition.Row,
                                               RowEnd);
            this.StartPosition = new CellPosition(newRowStart, 0);
            this.RowEnd = newRowEnd;
            return;
        }

        RowEnd += dRow;
        StartPosition = new CellPosition(StartPosition.Row + dRow, 0);
        return;
    }

    public IRange Copy()
    {
        return new RowRange(StartPosition.Row, RowEnd);
    }

    public IFixedSizeRange GetIntersection(IFixedSizeRange? range)
    {
        var rowFixed = new Range(StartPosition.Row, RowEnd, -int.MaxValue, int.MaxValue);
        return rowFixed.GetIntersection(range);
    }

    public void ExtendTo(int row, int col, IFixedSizeRange? rangeLimit = null)
    {
        if (rangeLimit == null)
        {
            RowEnd = row;
            return;
        }

        var newRowEnd = SheetMath
            .ClampInt(rangeLimit.StartPosition.Row, rangeLimit.EndPosition.Row, row);

        RowEnd = row;
    }
}