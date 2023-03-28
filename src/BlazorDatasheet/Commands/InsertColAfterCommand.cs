using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

/// <summary>
/// Command for inserting a column in to the sheet
/// </summary>
public class InsertColAfterCommand : IUndoableCommand
{
    private readonly int _colIndex;

    /// <summary>
    /// Command for inserting a column into the sheet.
    /// </summary>
    /// <param name="colIndex">The index that the column will be inserted AFTER.</param>
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