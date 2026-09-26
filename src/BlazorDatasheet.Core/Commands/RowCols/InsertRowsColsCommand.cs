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

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowsColsCommand : BaseCommand, IUndoableCommand
{
    private readonly int _index;
    private readonly int _count;
    private readonly Axis _axis;

    private readonly List<(IRowColShiftingStore Store, object? RestoreData)> _storeRestoreData = new();
    private RowColInfoRestoreData _rowColInfoRestoreData = null!;

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

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.Can(_axis == Axis.Row ? SheetOperation.InsertRows : SheetOperation.InsertColumns);

    protected override bool ExecuteCore(Sheet sheet)
    {
        using var updates = sheet.SuspendUpdates();
        sheet.Add(_axis, _count);

        _storeRestoreData.Clear();
        foreach (var store in sheet.ShiftingStores)
            _storeRestoreData.Add((store, store.InsertRowColAt(_index, _count, _axis)));

        _rowColInfoRestoreData = sheet.GetRowColStore(_axis).InsertImpl(_index, _count);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        using var updates = sheet.SuspendUpdates();
        sheet.Remove(_axis, _count);

        foreach (var (store, restoreData) in _storeRestoreData)
            store.Restore(restoreData);

        sheet.GetRowColStore(_axis).Restore(_rowColInfoRestoreData);

        sheet.GetRowColStore(_axis).EmitRemoved(_index, _count);

        IRegion dirtyRegion = _axis == Axis.Col
            ? new ColumnRegion(_index, sheet.NumCols)
            : new RowRegion(_index, sheet.NumRows);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}