using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events;

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
    public CellChangedFormat(int row, int col, CellFormat? oldFormat, CellFormat? newFormat)
    {
        Row = row;
        Col = col;
        OldFormat = oldFormat;
        NewFormat = newFormat;
    }

    public int Row { get; }
    public int Col { get; }
    public CellFormat? OldFormat { get; }
    public CellFormat? NewFormat { get; }
}