using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

internal class InsertRowAtCommand : IUndoableCommand
{
    private readonly int _index;

    public InsertRowAtCommand(int index)
    {
        _index = index;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.InsertRowAtImpl(_index);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        // Update for redo
        sheet.RemoveRowAtImpl(_index + 1);
        return true;
    }
}