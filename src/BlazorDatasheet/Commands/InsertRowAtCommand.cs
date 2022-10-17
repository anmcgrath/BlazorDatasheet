using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

internal class InsertRowAtCommand : IUndoableCommand
{
    private readonly int _index;
    private Row _rowToInsert;

    public InsertRowAtCommand(int index, Row? row)
    {
        _index = index;
        _rowToInsert = row;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.InsertRowAtImpl(_index, _rowToInsert);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        // Update for redo
        _rowToInsert = sheet.Rows[_index];
        sheet.RemoveRowAtImpl(_index);
        return true;
    }
}