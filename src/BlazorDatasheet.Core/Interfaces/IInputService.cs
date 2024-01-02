using BlazorDatasheet.Core.Events.Input;

namespace BlazorDatasheet.Core.Interfaces;

public interface IInputService
{
    /// <summary>
    /// Fires when the input device enters over a cell.
    /// </summary>
    public event EventHandler<InputOverCellEventArgs> InputOverCell;

    /// <summary>
    /// The updated X position (relative to the top left of sheet)
    /// </summary>
    public double CurrentX { get; }

    /// <summary>
    /// The updated Y position (relative to the top left of sheet)
    /// </summary>
    public double CurrentY { get; }

    /// <summary>
    /// The current row of the input device.
    /// </summary>
    public int CurrentRow { get; }

    /// <summary>
    /// The current col of the input device
    /// </summary>
    public int CurrentCol { get; }
}