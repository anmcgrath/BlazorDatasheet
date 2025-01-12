using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands.RowCols;

public class RemoveRowColsCommand : RegionCommand
{
    public int Index { get; }
    public Axis Axis { get; }
    public int Count { get; }

    private RegionRestoreData<int> _validatorRestoreData = null!;
    private RegionRestoreData<ConditionalFormatAbstractBase> _cfRestoreData = null!;
    private RowColInfoRestoreData _rowColInfoRestore = null!;
    private CellStoreRestoreData _cellStoreRestoreData = null!;
    private MergeableIntervalStoreRestoreData<OverwritingValue<List<IFilter>?>> _filterRestoreData = null!;

    // The actual number of rows removed (takes into account num of rows/columns in sheet)
    private int _nRemoved;

    /// <summary>
    /// Command to remove the row or column at the index given.
    /// </summary>
    /// <param name="index">The index to remove.</param>
    /// <param name="axis"></param>
    /// <param name="count">The total number to remove</param>
    public RemoveRowColsCommand(int index, Axis axis, int count = 1) : base(axis == Axis.Col
        ? new ColumnRegion(index, index + count - 1)
        : new RowRegion(index, index + count - 1))
    {
        Index = index;
        Axis = axis;
        Count = count;
    }

    protected override bool DoExecute(Sheet sheet)
    {
        if (Index >= sheet.GetSize(Axis))
            return false;

        if (Count <= 0)
            return false;

        _nRemoved = Math.Min(sheet.GetSize(Axis) - Index + 1, Count);
        sheet.Remove(Axis, _nRemoved);

        _cellStoreRestoreData = sheet.Cells.RemoveRowColAt(Index, _nRemoved, Axis);
        _rowColInfoRestore = sheet.GetRowColStore(Axis).RemoveImpl(Index, Index + _nRemoved - 1);
        _validatorRestoreData = sheet.Validators.Store.RemoveRowColAt(Index, _nRemoved, Axis);
        _cfRestoreData = sheet.ConditionalFormats.RemoveRowColAt(Index, _nRemoved, Axis);

        if (Axis == Axis.Col)
            _filterRestoreData = sheet.Columns.Filters.Store.Delete(Index, Index + _nRemoved - 1);

        IRegion dirtyRegion = Axis == Axis.Col ? new ColumnRegion(Index, sheet.NumCols-1) :
            new RowRegion(Index, sheet.NumRows - 1);
        sheet.MarkDirty(dirtyRegion);

        return true;
    }

    public override bool CanExecute(Sheet sheet)
    {
        if (Index >= sheet.GetSize(Axis))
            return false;

        if (Count <= 0)
            return false;

        return true;
    }

    protected override bool DoUndo(Sheet sheet)
    {
        sheet.Add(Axis, _nRemoved);
        sheet.Validators.Store.Restore(_validatorRestoreData);
        sheet.Cells.Restore(_cellStoreRestoreData);
        sheet.GetRowColStore(Axis).Restore(_rowColInfoRestore);
        sheet.ConditionalFormats.Restore(_cfRestoreData);

        if (Axis == Axis.Col)
            sheet.Columns.Filters.Store.Restore(_filterRestoreData);
        sheet.GetRowColStore(Axis).EmitInserted(Index, _nRemoved);

        IRegion dirtyRegion = Axis == Axis.Col
            ? new ColumnRegion(Index, sheet.NumCols)
            : new RowRegion(Index, sheet.NumRows);
        sheet.MarkDirty(dirtyRegion);
        return true;
    }
}