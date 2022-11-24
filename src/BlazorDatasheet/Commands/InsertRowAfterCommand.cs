using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

internal class InsertRowAfterCommand : IUndoableCommand
{
    private readonly int _index;

    public InsertRowAfterCommand(int index)
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