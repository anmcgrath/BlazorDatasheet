namespace BlazorDatasheet.Model;

public class Range
{
    public int RowStart { get; set; }
    public int ColStart { get; set; }
    public int RowEnd { get; set; }
    public int ColEnd { get; set; }

    public Range(int row, int col) : this(row, row, col, col)
    {
    }

    public Range(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        RowStart = rowStart;
        RowEnd = rowEnd;
        ColStart = colStart;
        ColEnd = colEnd;
    }

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

    public void Constrain(int rows, int cols)
    {
        RowStart = Constrain(0, rows - 1, RowStart);
        RowEnd = Constrain(0, rows - 1, RowEnd);
        ColStart = Constrain(0, cols - 1, ColStart);
        ColEnd = Constrain(0, cols - 1, ColEnd);
    }

    private int Constrain(int min, int max, int val)
    {
        if (val < min)
            return min;
        if (val > max)
            return max;
        return val;
    }
}