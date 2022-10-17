using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

/// <summary>
/// A column range. This range does not have an end position.
/// </summary>
public class RowRange : IRange
{
    public CellPosition Start { get; private set; }
    public int RowEnd { get; private set; }

    public RowRange(int rowStart, int rowEnd)
    {
        Start = new CellPosition(rowStart, 0);
        RowEnd = rowEnd;
    }

    public bool Contains(int row, int col)
    {
        var r0 = Math.Min(Start.Row, RowEnd);
        var r1 = Math.Max(Start.Row, RowEnd);
        return row >= r0 && row <= r1;
    }

    public bool SpansCol(int col) => true;

    public bool SpansRow(int row) => Contains(row, 0);

    public IRange Collapse() => new Range(Start.Row, Start.Col);

    public void Move(int dRow, int dCol, IFixedSizeRange? rangeLimit = null)
    {
        if (rangeLimit != null)
        {
            var newRowStart = SheetMath.ClampInt(rangeLimit.Start.Row, rangeLimit.End.Row,
                                                 Start.Row + dRow);
            var newRowEnd = SheetMath.ClampInt(rangeLimit.Start.Row, rangeLimit.End.Row,
                                               RowEnd);
            this.Start = new CellPosition(newRowStart, 0);
            this.RowEnd = newRowEnd;
            return;
        }

        RowEnd += dRow;
        Start = new CellPosition(Start.Row + dRow, 0);
        return;
    }

    public IRange Copy()
    {
        return new RowRange(Start.Row, RowEnd);
    }

    public IFixedSizeRange GetIntersection(IFixedSizeRange? range)
    {
        var rowFixed = new Range(Start.Row, RowEnd, -int.MaxValue, int.MaxValue);
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
            .ClampInt(rangeLimit.Start.Row, rangeLimit.End.Row, row);

        RowEnd = row;
    }
}