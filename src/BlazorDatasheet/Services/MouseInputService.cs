using BlazorDatasheet.Core.Events.Input;
using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Services;

public class MouseInputService : IInputService
{
    public event EventHandler<InputOverCellEventArgs>? InputOverCell;
    public double CurrentX { get; private set; }
    public double CurrentY { get; private set; }
    public int CurrentRow { get; private set; }
    public int CurrentCol { get; private set; }

    internal InputOverCellEventArgs OnMouseOverCell(int row, int col)
    {
        var args = new InputOverCellEventArgs(row, col);
        InputOverCell?.Invoke(this, args);
        return args;
    }

    internal void UpdateCurrentMousePosition(double x, double y)
    {
        CurrentX = x;
        CurrentY = y;
    }

    internal void UpdateCurrentRowCol(int row, int col)
    {
        CurrentRow = row;
        CurrentCol = col;
    }
}