using System.ComponentModel;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Data;

/// <summary>
/// Raised before cells, rows or columns are moved, whether by dragging the selection or by
/// <see cref="Cells.CellStore.Move(SheetRange, CellPosition, Axis?)"/> or <see cref="RowColInfoStore.Move"/>.
/// Set <see cref="CancelEventArgs.Cancel"/> to stop the move, or change <see cref="To"/> or <see cref="ShiftAxis"/>
/// to move the cells somewhere else.
/// </summary>
public class BeforeRangeMoveEventArgs : CancelEventArgs
{
    /// <summary>
    /// The cells being moved. They may be on another sheet of the workbook than the one they are moved to.
    /// </summary>
    public SheetRange Source { get; }

    /// <summary>
    /// With no <see cref="ShiftAxis"/>, where the top-left of the moved block lands. Otherwise the cell (or, for
    /// whole rows and columns, the row or column) the block is inserted before, as it is before the move.
    /// </summary>
    public CellPosition To { get; set; }

    /// <summary>
    /// Null to overwrite the destination, or the axis along which the neighbouring cells shift to make room.
    /// Whole rows are moved along <see cref="Axis.Row"/> and whole columns along <see cref="Axis.Col"/>.
    /// </summary>
    public Axis? ShiftAxis { get; set; }

    /// <summary>
    /// Whether the move was made by dragging the selection, rather than through the API.
    /// </summary>
    public bool IsUserDrag { get; }

    public BeforeRangeMoveEventArgs(SheetRange source, CellPosition to, Axis? shiftAxis, bool isUserDrag)
    {
        Source = source;
        To = to;
        ShiftAxis = shiftAxis;
        IsUserDrag = isUserDrag;
    }
}
