using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands.RowCols;

/// <summary>
/// Command for inserting a row into the sheet.
/// </summary>
internal class InsertRowsColsCommand : RegionCommand
{
    public int Index { get; }
    public int Count { get; }
    public Axis Axis { get; }

    private RegionRestoreData<int> _validatorRestoreData = null!;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData = null!;
    private CellStoreRestoreData _cellStoreRestoreData = null!;
    private RowColInfoRestoreData _rowColInfoRestoreData = null!;
    private MergeableIntervalStoreRestoreData<OverwritingValue<List<IFilter>?>> _filterRestoreData = null!;

    /// <summary>
    /// Command for inserting a row into the sheet.
    /// </summary>
    /// <param name="index">The index that the row/column will be inserted at.</param>
    /// <param name="count">The number to insert</param>
    /// <param name="axis">Which axis to insert into the sheet</param>
    public InsertRowsColsCommand(int index, int count, Axis axis) : base(axis == Axis.Col
        ? new ColumnRegion(index, index + count - 1)
        : new RowRegion(index, index + count - 1))
    {
        Index = index;
        Count = count;
        Axis = axis;
    }

    protected override bool DoExecute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.Add(Axis, Count);
        _validatorRestoreData = sheet.Validators.Store.InsertRowColAt(Index, Count, Axis);
        _cellStoreRestoreData = sheet.Cells.InsertRowColAt(Index, Count, Axis);
        _cfRestoreData = sheet.ConditionalFormats.InsertRowColAt(Index, Count, Axis);
        _rowColInfoRestoreData = sheet.GetRowColStore(Axis).InsertImpl(Index, Count);

        if (Axis == Axis.Col)
        {
            _filterRestoreData = sheet.Columns.Filters.Store.InsertAt(Index, Count);
        }

        sheet.ScreenUpdating = true;
        return true;
    }

    public override bool CanExecute(Sheet sheet) => true;

    protected override bool DoUndo(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.Remove(Axis, Count);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.ConditionalFormats.Restore(_cfRestoreData);
        sheet.GetRowColStore(Axis).Restore(_rowColInfoRestoreData);
        if (Axis == Axis.Col)
            sheet.Columns.Filters.Store.Restore(_filterRestoreData);

        sheet.GetRowColStore(Axis).EmitRemoved(Index, Count);

        IRegion dirtyRegion = Axis == Axis.Col
            ? new ColumnRegion(Index, sheet.NumCols)
            : new RowRegion(Index, sheet.NumRows);
        sheet.MarkDirty(dirtyRegion);
        sheet.ScreenUpdating = true;
        return true;
    }
}