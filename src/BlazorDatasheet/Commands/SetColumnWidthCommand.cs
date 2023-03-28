using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetColumnWidthCommand : IUndoableCommand
{
    private readonly int _col;
    private readonly double _width;
    private double _oldWidth;

    /// <summary>
    /// Command that changes the column width to the specified amount
    /// </summary>
    /// <param name="col">The index of the column to change.</param>
    /// <param name="width">The new width of the column, in pixels</param>
    public SetColumnWidthCommand(int col, double width)
    {
        _col = col;
        _width = width;
    }

    public bool Execute(Sheet sheet)
    {
        _oldWidth = sheet.LayoutProvider.ComputeWidth(_col, 1);
        sheet.SetColumnWidthImpl(_col, _width);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetColumnWidthImpl(_col, _oldWidth);
        return true;
    }
}