namespace BlazorDatasheet.Data;

public struct CellPosition : IEquatable<CellPosition>
{
    public int Row { get; set; }
    public int Col { get; set; }
    public bool Sticky { get; }
    public bool IsInvalid { get; } = false;

    public CellPosition(int row, int col, bool sticky = false)
    {
        Row = row;
        Col = col;
        Sticky = sticky;
        if (row < 0 || col < 0)
            IsInvalid = true;
    }

    public bool Equals(int row, int col)
    {
        return Row == row && Col == col;
    }

    public bool Equals(CellPosition other)
    {
        return Row == other.Row && Col == other.Col;
    }

    public override bool Equals(object? obj)
    {
        return obj is CellPosition other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Row, Col);
    }

    /// <summary>
    /// Returns either the row or the column, depending on the axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int Get(Axis axis)
    {
        return axis == Axis.Col ? Col : Row;
    }

    public override string ToString()
    {
        return $"({Col}, {Row})";
    }
}