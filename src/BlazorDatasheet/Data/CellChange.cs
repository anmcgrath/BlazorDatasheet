namespace BlazorDatasheet.Data;

/// <summary>
/// Describes a change to a cell's value
/// </summary>
public class CellChange
{
    /// <summary>
    /// The cell's row.
    /// </summary>
    public int Row { get; }
    /// <summary>
    /// The cell's column
    /// </summary>
    public int Col { get; }
    /// <summary>
    /// The new value of the cell
    /// </summary>
    public object? NewValue { get; set; }

    public CellChange(int row, int col, object? newValue)
    {
        Row = row;
        Col = col;
        NewValue = newValue;
    }
}