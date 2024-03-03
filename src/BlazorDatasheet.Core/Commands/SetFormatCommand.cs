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
    private readonly bool _clearSurroundingBorders;
    private readonly IRegion _region;

    private RowColFormatRestoreData? _colFormatRestoreData;
    private RowColFormatRestoreData? _rowFormatRestoreData;
    private CellStoreRestoreData? _cellFormatRestoreData;
    private List<IUndoableCommand> _borderCommands = new();

    /// <summary>
    /// Command to set the format of the range given. The cell format is merged into the existing format, so that
    /// only properties that are specifically defined in cellFormat are changed.
    /// </summary>
    /// <param name="region">The region to set the format for. Can be a cell, column or row range.</param>
    /// <param name="cellFormat">The new cell format.</param>
    /// <param name="clearSurroundingBorders">Whether to clear surrounding borders when setting this format</param>
    public SetFormatCommand(IRegion region, CellFormat cellFormat, bool clearSurroundingBorders = true)
    {
        _cellFormat = cellFormat;
        _clearSurroundingBorders = clearSurroundingBorders;
        _region = region.Clone();
    }

    public bool Execute(Sheet sheet)
    {
        sheet.BatchUpdates();
        _borderCommands = new();

        if (_region is ColumnRegion columnRegion)
            _colFormatRestoreData = sheet.Columns.SetColumnFormatImpl(_cellFormat, columnRegion);
        else if (_region is RowRegion rowRegion)
            _rowFormatRestoreData = sheet.Rows.SetRowFormatImpl(_cellFormat, rowRegion);
        else
        {
            _cellFormatRestoreData = sheet.Cells.MergeFormatImpl(_region, _cellFormat);

            if (_clearSurroundingBorders)
            {
                // left
                IRegion leftRegion = new Region(_region.Top, _region.Bottom, _region.Left - 1, _region.Left - 1);
                leftRegion = sheet.Region.GetIntersection(leftRegion);
                if (leftRegion != null)
                {
                    var cf = new CellFormat()
                    {
                        BorderRight = _cellFormat.BorderLeft?.Clone()
                    };
                    var cmd = new SetFormatCommand(leftRegion, cf, false);
                    cmd.Execute(sheet);
                    _borderCommands.Add(cmd);
                }

                IRegion topRegion = new Region(_region.Top - 1, _region.Top - 1, _region.Left, _region.Right);
                topRegion = sheet.Region.GetIntersection(topRegion);
                if (topRegion != null)
                {
                    var cf = new CellFormat()
                    {
                        BorderBottom = _cellFormat.BorderTop?.Clone()
                    };
                    var cmd = new SetFormatCommand(topRegion, cf, false);
                    cmd.Execute(sheet);
                    _borderCommands.Add(cmd);
                }
            }
        }

        sheet.EndBatchUpdates();

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.BatchUpdates();
        foreach (var cmd in _borderCommands)
            cmd.Undo(sheet);

        if (_colFormatRestoreData != null)
            Restore(sheet, _colFormatRestoreData, sheet.Columns.ColFormats);
        if (_rowFormatRestoreData != null)
            Restore(sheet, _rowFormatRestoreData, sheet.Rows.RowFormats);
        if (_cellFormatRestoreData != null)
            sheet.Cells.Restore(_cellFormatRestoreData);

        sheet.MarkDirty(_region);
        sheet.EndBatchUpdates();
        return true;
    }

    private void Restore(Sheet sheet, RowColFormatRestoreData restoreData, MergeableIntervalStore<CellFormat> store)
    {
        foreach (var added in restoreData.IntervalsAdded)
            store.Clear(added);
        store.AddRange(restoreData.IntervalsRemoved.Where(x => x.Data != null));
        foreach (var cellRestore in restoreData.CellFormatRestoreData)
            sheet.Cells.Restore(cellRestore);
    }
}