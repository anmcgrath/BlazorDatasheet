using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetColumnWidthCommand : IUndoableCommand
{
    private readonly int _col;
    private readonly double _amount;
    private double _oldWidth;

    public SetColumnWidthCommand(int col, double amount)
    {
        _col = col;
        _amount = amount;
    }

    public bool Execute(Sheet sheet)
    {
        _oldWidth = sheet.LayoutProvider.ComputeWidth(_col, 1);
        sheet.SetColumnWidthImpl(_col, _amount);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetColumnWidthImpl(_col, _oldWidth);
        return true;
    }
}