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

    private List<CellChangedFormat> _removedCellFormats;
    private List<OrderedInterval<CellFormat>> _rowFormatRestoreData;
    private RegionRestoreData<bool> _mergeRestoreData;
    private RegionRestoreData<int> _validatorRestoreData;
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

        _cellStoreRestoreData = sheet.Cells.RemoveRowAt(_rowIndex, _nRowsRemoved);
        _rowFormatRestoreData = sheet.RowFormats.Remove(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        sheet.RowFormats.ShiftLeft(_rowIndex, _nRowsRemoved);
        
        _mergeRestoreData = sheet.Cells.Merges.Store.RemoveRows(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        _validatorRestoreData = sheet.Cells.Validation.Store.RemoveRows(_rowIndex, _rowIndex + _nRowsRemoved - 1);
        return sheet.RemoveRowAtImpl(_rowIndex, _nRowsRemoved);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Cells.Merges.Store.InsertRows(_rowIndex, _nRowsRemoved);
        sheet.Cells.Merges.Store.Restore(_mergeRestoreData);

        sheet.Cells.Validation.Store.InsertRows(_rowIndex, _nRowsRemoved);
        sheet.Cells.Validation.Store.Restore(_validatorRestoreData);

        sheet.Cells.InsertRowAt(_rowIndex, _nRows);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.InsertRowAtImpl(_rowIndex);

        sheet.RowFormats.ShiftRight(_rowIndex, _nRowsRemoved);
        sheet.RowFormats.AddRange(_rowFormatRestoreData);

        return true;
    }
}