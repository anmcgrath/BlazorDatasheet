using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Formatting;

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
            _colFormatRestoreData = sheet.Columns.SetFormatImpl(_cellFormat, columnRegion.Left, columnRegion.Right);
        else if (_region is RowRegion rowRegion)
            _rowFormatRestoreData = sheet.Rows.SetFormatImpl(_cellFormat, rowRegion.Top, rowRegion.Bottom);
        else
        {
            var region = sheet.Region.GetIntersection(_region);
            if (region != null)
                _cellFormatRestoreData = sheet.Cells.MergeFormatImpl(region, _cellFormat);
        }

        UpdateSurroundingBorders(sheet);
        sheet.EndBatchUpdates();

        return true;
    }

    private void UpdateSurroundingBorders(Sheet sheet)
    {
        if (!_clearSurroundingBorders)
            return;

        IRegion? above = null;
        IRegion? left = null;

        if (_region is ColumnRegion columnRegion)
            left = new ColumnRegion(_region.Left - 1);
        else if (_region is RowRegion rowRegion)
            above = new RowRegion(_region.Top - 1);
        else
        {
            left = new Region(_region.Top, _region.Bottom, _region.Left - 1, _region.Left - 1);
            above = new Region(_region.Top - 1, _region.Top - 1, _region.Left, _region.Right);
        }

        var cfLeft = new CellFormat() { BorderRight = _cellFormat?.BorderLeft?.Clone() };
        var cfAbove = new CellFormat() { BorderBottom = _cellFormat?.BorderTop?.Clone() };

        if (left != null)
            _borderCommands.Add(new SetFormatCommand(left, cfLeft, false));
        if (above != null)
            _borderCommands.Add(new SetFormatCommand(above, cfAbove, false));

        foreach (var cmd in _borderCommands)
            cmd.Execute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.BatchUpdates();
        foreach (var cmd in _borderCommands)
            cmd.Undo(sheet);

        if (_colFormatRestoreData != null)
            Restore(sheet, _colFormatRestoreData, sheet.Columns.Formats);
        if (_rowFormatRestoreData != null)
            Restore(sheet, _rowFormatRestoreData, sheet.Rows.Formats);
        if (_cellFormatRestoreData != null)
            sheet.Cells.Restore(_cellFormatRestoreData);

        sheet.MarkDirty(_region);
        sheet.EndBatchUpdates();
        return true;
    }

    private void Restore(Sheet sheet, RowColFormatRestoreData restoreData, MergeableIntervalStore<CellFormat> store)
    {
        store.Restore(restoreData.Format1DRestoreData);
        for (int i = restoreData.CellFormatRestoreData.Count - 1; i >= 0; i--)
        {
            var cellRestore = restoreData.CellFormatRestoreData[i];
            sheet.Cells.Restore(cellRestore);
        }
    }
}