using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowsAtCommand : IUndoableCommand
{
    private readonly int _index;
    private readonly int _nRows;

    private RegionRestoreData<int> _validatorRestoreData;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData;
    private CellStoreRestoreData _cellStoreRestoreData;

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
        _validatorRestoreData = sheet.Validators.Store.InsertRows(_index, _nRows);
        _cellStoreRestoreData = sheet.Cells.InsertRowAt(_index, _nRows);
        _cfRestoreData = sheet.ConditionalFormats.InsertRowAtImpl(_index, _nRows);
        sheet.AddRows(_nRows);
        sheet.Rows.InsertImpl(_index, _nRows);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.RemoveRows(_nRows);
        sheet.Rows.RemoveRowsImpl(_index, _index + _nRows - 1);
        return true;
    }
}