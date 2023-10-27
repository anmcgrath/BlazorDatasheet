using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowsAtCommand : IUndoableCommand
{
    private readonly int _index;
    private readonly int _nRows;

    /// <summary>
    /// Command for inserting a row into the sheet.
    /// </summary>
    /// <param name="index">The index that the row will be inserted at.</param>
    /// <param name="nRows">The number of rows to insert</param>
    public InsertRowsAtCommand(int index, int nRows = 1)
    {
        _index = index;
        _nRows = nRows;
    }

    public bool Execute(Sheet sheet)
    {
        sheet.Validators.Store.InsertRows(_index, _nRows);
        sheet.Cells.InsertRowAt(_index, _nRows);
        sheet.InsertRowAtImpl(_index, _nRows);
        sheet.Rows.InsertImpl(_index, _nRows);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Validators.Store.RemoveRows(_index, _index + _nRows - 1);
        sheet.Cells.RemoveRowAt(_index, _nRows);
        sheet.RemoveRowAtImpl(_index, _nRows);
        sheet.Rows.Cut(_index, _index + _nRows - 1);
        return true;
    }
}