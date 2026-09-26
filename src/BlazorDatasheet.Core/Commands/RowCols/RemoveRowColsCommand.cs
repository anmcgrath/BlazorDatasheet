using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Metadata;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class RemoveRowColsCommand : BaseCommand, IUndoableCommand
{
    private readonly int _index;
    private readonly Axis _axis;
    private readonly int _count;

    private readonly List<(IRowColShiftingStore Store, object? RestoreData)> _storeRestoreData = new();
    private RowColInfoRestoreData _rowColInfoRestore = null!;

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

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(
        _axis == Axis.Row ? SheetOperation.DeleteRows : SheetOperation.DeleteColumns,
        _axis == Axis.Row
            ? new RowRegion(_index, _index + _count - 1)
            : new ColumnRegion(_index, _index + _count - 1));

    protected override bool CanExecuteCore(Sheet sheet)
    {
        if (_index >= sheet.GetSize(_axis))
            return false;

        if (_count <= 0)
            return false;

        return true;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        if (_index >= sheet.GetSize(_axis))
            return false;

        if (_count <= 0)
            return false;

        using var updates = sheet.SuspendUpdates();
        _nRemoved = Math.Min(sheet.GetSize(_axis) - _index, _count);
        sheet.Remove(_axis, _nRemoved);

        _storeRestoreData.Clear();
        foreach (var store in sheet.ShiftingStores)
            _storeRestoreData.Add((store, store.RemoveRowColAt(_index, _nRemoved, _axis)));

        _rowColInfoRestore = sheet.GetRowColStore(_axis).RemoveImpl(_index, _index + _nRemoved - 1);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        using var updates = sheet.SuspendUpdates();
        sheet.Add(_axis, _nRemoved);

        foreach (var (store, restoreData) in _storeRestoreData)
            store.Restore(restoreData);

        sheet.GetRowColStore(_axis).Restore(_rowColInfoRestore);

        sheet.GetRowColStore(_axis).EmitInserted(_index, _nRemoved);

        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(_index, sheet.NumCols)
            : new RowRegion(_index, sheet.NumRows);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}
