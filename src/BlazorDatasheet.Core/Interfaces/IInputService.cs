using BlazorDatasheet.Core.Events.Input;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Interfaces;

public interface IInputService
{
    /// <summary>
    /// Fires when the input device enters over a cell.
    /// </summary>
    public event EventHandler<InputOverCellEventArgs> InputOverCell;

    /// <summary>
    /// The current position (relative to the top left of sheet) of the input device
    /// </summary>
    public Task<Point2d> GetInputPositionAsync();
}