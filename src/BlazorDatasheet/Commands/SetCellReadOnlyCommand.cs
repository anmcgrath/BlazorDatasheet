using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetCellReadOnlyCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly bool _readOnly;
    private bool _oldReadOnly;


    /// <summary>
    /// Command that changes the column width to the specified amount
    /// </summary>
    /// <param name="col">The index of the column to change.</param>
    /// <param name="width">The new width of the column, in pixels</param>
    public SetCellReadOnlyCommand(int row, int col, bool readOnly)
    {
        _row = row;
        _col = col;
        _readOnly = readOnly;
    }

    public bool Execute(Sheet sheet)
    {
        _oldReadOnly = sheet.GetCell(_row, _col)?.IsReadOnly ?? false;
        sheet.SetCellReadOnlyImpl(_row, _col, _readOnly);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetCellReadOnlyImpl(_row, _col, _oldReadOnly);
        return true;
    }
}