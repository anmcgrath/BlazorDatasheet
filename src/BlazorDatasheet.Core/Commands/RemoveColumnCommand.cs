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

    private RegionRestoreData<int> _validatorRestoreData;
    private CellStoreRestoreData _cellStoreRestoreData;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData;
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

        sheet.RemoveCols(_nColsRemoved);
        _cellStoreRestoreData = sheet.Cells.RemoveColAt(_columnIndex, _nColsRemoved);
        _columnInfoRestoreData = sheet.Columns.RemoveColumnsImpl(_columnIndex, _columnIndex + _nColsRemoved - 1);
        _validatorRestoreData = sheet.Validators.Store.RemoveCols(_columnIndex, _columnIndex + _nColsRemoved - 1);
        _cfRestoreData = sheet.ConditionalFormats.RemoveColAt(_columnIndex, _nColsRemoved);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        // Insert column back in and set all the values that we removed
        sheet.AddCols(_nColsRemoved);
        sheet.Columns.InsertImpl(_columnIndex, _nColsRemoved);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Columns.Restore(_columnInfoRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.MarkDirty(new ColumnRegion(_columnIndex, sheet.NumCols));

        return true;
    }
}