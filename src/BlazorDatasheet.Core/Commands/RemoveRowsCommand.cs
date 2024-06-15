using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands;

public class RemoveRowsCommand : IUndoableCommand
{
    private readonly int _rowIndex;
    private readonly int _nRows;

    private RegionRestoreData<int> _validatorRestoreData;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData;
    private RowInfoStoreRestoreData _rowInfoStoreRestore;
    private CellStoreRestoreData _cellStoreRestoreData;

    // The actual number of rows removed (takes into account num of rows in sheet)
    private int _nRowsRemoved;

    /// <summary>
    /// Command to remove the row at the index given.
    /// </summary>
    /// <param name="rowIndex">The row to remove.</param>
    /// <param name="nRows">The number of rows to remove</param>
    public RemoveRowsCommand(int rowIndex, int nRows = 1)
    {
        _rowIndex = rowIndex;
        _nRows = nRows;
    }

    public bool Execute(Sheet sheet)
    {
        if (_rowIndex >= sheet.NumRows)
            return false;
        if (_nRows <= 0)
            return false;
        _nRowsRemoved = Math.Min(sheet.NumRows - _rowIndex + 1, _nRows);
        sheet.RemoveRows(_nRowsRemoved);

        _cellStoreRestoreData = sheet.Cells.RemoveRowAt(_rowIndex, _nRowsRemoved);
        _rowInfoStoreRestore = sheet.Rows.RemoveRowsImpl(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        _validatorRestoreData = sheet.Validators.Store.RemoveRows(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        _cfRestoreData = sheet.ConditionalFormats.RemoveRowAt(_rowIndex, _nRowsRemoved);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.AddRows(_nRowsRemoved);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.Rows.InsertImpl(_rowIndex, _nRowsRemoved);
        sheet.Rows.Restore(_rowInfoStoreRestore);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.MarkDirty(new RowRegion(_rowIndex, sheet.NumRows));
        return true;
    }
}