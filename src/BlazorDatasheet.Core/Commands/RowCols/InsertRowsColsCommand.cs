using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands.RowCols;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowsColsCommand : IUndoableCommand
{
    private readonly int _index;
    private readonly int _count;
    private readonly Axis _axis;

    private RegionRestoreData<int> _validatorRestoreData;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData;
    private CellStoreRestoreData _cellStoreRestoreData;

    /// <summary>
    /// Command for inserting a row into the sheet.
    /// </summary>
    /// <param name="index">The index that the row/column will be inserted at.</param>
    /// <param name="count">The number to insert</param>
    /// <param name="axis">Which axis to insert into the sheet</param>
    public InsertRowsColsCommand(int index, int count, Axis axis)
    {
        _index = index;
        _count = count;
        _axis = axis;
    }

    public bool Execute(Sheet sheet)
    {
        _validatorRestoreData = sheet.Validators.Store.InsertRowColAt(_index, _count, _axis);
        _cellStoreRestoreData = sheet.Cells.InsertRowColAt(_index, _count, _axis);
        _cfRestoreData = sheet.ConditionalFormats.InsertRowColAt(_index, _count, _axis);
        sheet.Add(_axis, _count);
        sheet.GetRowColStore(_axis).InsertImpl(_index, _count);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.Remove(_axis, _count);
        sheet.GetRowColStore(_axis).RemoveImpl(_index, _index + _count - 1);
        return true;
    }
}