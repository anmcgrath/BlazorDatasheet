namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Describes a change to a cell's value
/// </summary>
public class CellValueChange
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
    public object? NewValue { get; }

    public CellValueChange(int row, int col, object? newValue)
    {
        Row = row;
        Col = col;
        NewValue = newValue;
    }
}