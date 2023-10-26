using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private readonly int _nCols;

    private RegionRestoreData<bool> _mergeRestoreData;
    private RegionRestoreData<int> _validatorRestoreData;
    private CellStoreRestoreData _cellStoreRestoreData;
    private List<OrderedInterval<CellFormat>> _colFormatRestoreData;
    private int _nColsRemoved;
    private ColumnInfoRestoreData _columnInfoRestoreData;

    /// <summary>
    /// Command for removing a column at the index given.
    /// </summary>
    /// <param name="columnIndex">The column to remove.</param>
    public RemoveColumnCommand(int columnIndex, int nCols)
    {
        _columnIndex = columnIndex;
        _nCols = nCols;
    }

    public bool Execute(Sheet sheet)
    {
        if (_columnIndex >= sheet.NumCols)
            return false;
        if (_nCols <= 0)
            return false;
        _nColsRemoved = Math.Min(sheet.NumCols - _columnIndex + 1, _nCols);

        if (_nColsRemoved == 0)
            return false;

        _cellStoreRestoreData = sheet.Cells.RemoveColAt(_columnIndex, _nColsRemoved);
        _colFormatRestoreData = sheet.ColFormats.Remove(_columnIndex, _columnIndex + _nColsRemoved - 1);
        sheet.ColFormats.ShiftLeft(_columnIndex, _nColsRemoved);

        RemoveColumnHeadingAndStoreWidth(sheet);

        _mergeRestoreData = sheet.Cells.Merges.Store.RemoveCols(_columnIndex, _columnIndex + _nColsRemoved - 1);
        _validatorRestoreData = sheet.Cells.Validation.Store.RemoveCols(_columnIndex, _columnIndex + _nColsRemoved - 1);
        return sheet.RemoveColImpl(_columnIndex, _nColsRemoved);
    }

    private void RemoveColumnHeadingAndStoreWidth(Sheet sheet)
    {
        _columnInfoRestoreData = sheet.ColumnInfo.Cut(_columnIndex, _columnIndex + _nColsRemoved - 1);
    }

    public bool Undo(Sheet sheet)
    {
        // perform undos for merges, validation etc.
        UndoMerges(sheet);
        UndoValidation(sheet);

        // Insert column back in and set all the values that we removed
        sheet.InsertColAtImpl(_columnIndex, _nColsRemoved);

        sheet.Cells.InsertColAt(_columnIndex, _nColsRemoved);
        sheet.Cells.Restore(_cellStoreRestoreData);

        sheet.ColumnInfo.Insert(_columnIndex, _nColsRemoved);
        sheet.ColumnInfo.RestoreFromData(_columnInfoRestoreData);

        sheet.ColFormats.ShiftRight(_columnIndex, _nColsRemoved);
        sheet.ColFormats.AddRange(_colFormatRestoreData);

        return true;
    }


    private void UndoValidation(Sheet sheet)
    {
        sheet.Cells.Validation.Store.InsertCols(_columnIndex, _nColsRemoved);
        sheet.Cells.Validation.Store.Restore(_validatorRestoreData);
    }

    public void UndoMerges(Sheet sheet)
    {
        sheet.Cells.Merges.Store.InsertCols(_columnIndex, _nColsRemoved);
        sheet.Cells.Merges.Store.Restore(_mergeRestoreData);
    }
}