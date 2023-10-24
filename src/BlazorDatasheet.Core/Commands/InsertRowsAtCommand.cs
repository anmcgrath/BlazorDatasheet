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
        sheet.Merges.Store.InsertRows(_index, _nRows);
        sheet.Validation.Store.InsertRows(_index, _nRows);
        sheet.InsertRowAtImpl(_index, _nRows);
        sheet.RowFormats.ShiftRight(_index, _nRows);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Merges.Store.RemoveRows(_index, _index + _nRows - 1);
        sheet.Validation.Store.RemoveRows(_index, _index + _nRows - 1);
        sheet.RemoveRowAtImpl(_index, _nRows);
        sheet.RowFormats.ShiftLeft(_index, _nRows);
        return true;
    }
}