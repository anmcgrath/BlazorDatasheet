using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Commands;

public class SetFormatCommand : IUndoableCommand
{
    private readonly CellFormat _cellFormat;
    private readonly IRegion _region;

    private RowColFormatRestoreData? _colFormatRestoreData;
    private RowColFormatRestoreData? _rowFormatRestoreData;
    private CellStoreRestoreData? _cellFormatRestoreData;

    /// <summary>
    /// Command to set the format of the range given. The cell format is merged into the existing format, so that
    /// only properties that are specifically defined in cellFormat are changed.
    /// </summary>
    /// <param name="range">The range to set the format for. Can be a cell, column or row range.</param>
    /// <param name="cellFormat">The new cell format.</param>
    public SetFormatCommand(IRegion region, CellFormat cellFormat)
    {
        _cellFormat = cellFormat;
        _region = region.Clone();
    }

    public bool Execute(Sheet sheet)
    {
        if (_region is ColumnRegion columnRegion)
            _colFormatRestoreData = sheet.Columns.SetColumnFormatImpl(_cellFormat, columnRegion);
        else if (_region is RowRegion rowRegion)
            _rowFormatRestoreData = sheet.Rows.SetRowFormatImpl(_cellFormat, rowRegion);
        else
            _cellFormatRestoreData = sheet.Cells.MergeFormatImpl(_region, _cellFormat);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_colFormatRestoreData != null)
            Restore(sheet, _colFormatRestoreData, sheet.Columns.ColFormats);
        if (_rowFormatRestoreData != null)
            Restore(sheet, _rowFormatRestoreData, sheet.Rows.RowFormats);
        if (_cellFormatRestoreData != null)
            sheet.Cells.Restore(_cellFormatRestoreData);

        sheet.MarkDirty(_region);
        return true;
    }

    private void Restore(Sheet sheet, RowColFormatRestoreData restoreData, NonOverlappingIntervals<CellFormat> store)
    {
        foreach (var added in restoreData.IntervalsAdded)
            store.Remove(added);
        store.AddRange(restoreData.IntervalsRemoved.Where(x => x.Data != null));
        foreach (var cellRestore in restoreData.CellFormatRestoreData)
            sheet.Cells.Restore(cellRestore);
    }
}