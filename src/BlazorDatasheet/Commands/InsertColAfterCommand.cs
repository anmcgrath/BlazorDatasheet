using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class InsertColAfterCommand : IUndoableCommand
{
    private readonly int _colIndex;

    public InsertColAfterCommand(int colIndex)
    {
        _colIndex = colIndex;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.InsertColAfterImpl(_colIndex);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.RemoveColImpl(_colIndex + 1);
        return true;
    }
}