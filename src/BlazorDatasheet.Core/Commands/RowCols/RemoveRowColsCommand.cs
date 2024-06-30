using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class RemoveRowColsCommand : IUndoableCommand
{
    private readonly int _index;
    private readonly Axis _axis;
    private readonly int _count;

    private RegionRestoreData<int> _validatorRestoreData = null!;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData = null!;
    private RowColInfoRestoreData _rowColInfoRestore = null!;
    private CellStoreRestoreData _cellStoreRestoreData = null!;

    // The actual number of rows removed (takes into account num of rows/columns in sheet)
    private int _nRemoved;

    /// <summary>
    /// Command to remove the row or column at the index given.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    /// <param name="axis"></param>
    /// <param name="count">The total number to remove</param>
    public RemoveRowColsCommand(int index, Axis axis, int count = 1)
    {
        _index = index;
        _axis = axis;
        _count = count;
    }

    public bool Execute(Sheet sheet)
    {
        if (_index >= sheet.GetSize(_axis))
            return false;

        if (_count <= 0)
            return false;

        _nRemoved = Math.Min(sheet.GetSize(_axis) - _index + 1, _count);
        sheet.Remove(_axis, _nRemoved);

        _cellStoreRestoreData = sheet.Cells.RemoveRowColAt(_index, _nRemoved, _axis);
        _rowColInfoRestore = sheet.GetRowColStore(_axis).RemoveImpl(_index, _index + _nRemoved - 1);
        _validatorRestoreData = sheet.Validators.Store.RemoveRowColAt(_index, _nRemoved, _axis);
        _cfRestoreData = sheet.ConditionalFormats.RemoveRowColAt(_index, _nRemoved, _axis);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Add(_axis, _nRemoved);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.GetRowColStore(_axis).Restore(_rowColInfoRestore);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.GetRowColStore(_axis).EmitInserted(_index, _nRemoved);
        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(_index, sheet.NumCols)
            : new RowRegion(_index, sheet.NumRows);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}