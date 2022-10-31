using BlazorDatasheet.Data;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats;

public class FormatChangedEventArgs
{
    /// <summary>
    /// All the individual CELL that were made to the formats of ranges in this event.
    /// </summary>
    public IEnumerable<CellChangedFormat> CellsChanged { get; }

    public IEnumerable<ColumnRegion> ColumnsChanged { get; }
    public IEnumerable<RowRegion> RowsChanged { get; }

    public FormatChangedEventArgs(
        IEnumerable<CellChangedFormat> changedFormats,
        IEnumerable<ColumnRegion> columnsChanged,
        IEnumerable<RowRegion> rowsChanged)
    {
        CellsChanged = changedFormats;
        ColumnsChanged = columnsChanged;
        RowsChanged = rowsChanged;
    }
}

public class CellChangedFormat
{
    public CellChangedFormat(int row, int col, Format? oldFormat, Format? newFormat)
    {
        Row = row;
        Col = col;
        OldFormat = oldFormat;
        NewFormat = newFormat;
    }

    public int Row { get; }
    public int Col { get; }
    public Format? OldFormat { get; }
    public Format? NewFormat { get; }
}