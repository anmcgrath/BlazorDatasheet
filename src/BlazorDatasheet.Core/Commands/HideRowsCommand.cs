using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class HideRowsCommand : IUndoableCommand
{
    private int _start;
    private int _end;

    private SetRowHeightCommand _rowHeightCommand = null!;

    public HideRowsCommand(int start, int end)
    {
        _start = start;
        _end = end;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.Rows.HideRowsImpl(_start, _end - _start + 1);
        _rowHeightCommand = new SetRowHeightCommand(_start, _end, 0);
        _rowHeightCommand.Execute(sheet);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Rows.UnhideRowsImpl(_start, _end - _start + 1);
        _rowHeightCommand.Undo(sheet);
        return true;
    }
}