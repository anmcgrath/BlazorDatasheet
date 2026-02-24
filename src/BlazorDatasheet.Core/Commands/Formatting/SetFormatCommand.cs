using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Commands.Formatting;

public class SetFormatCommand : BaseCommand, IUndoableCommand
{
    private readonly CellFormat _cellFormat;
    private readonly bool _clearSurroundingBorders;
    public IRegion Region { get; }

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
        Region = region.Clone();
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        sheet.BatchUpdates();
        _borderCommands = new();

        if (Region is ColumnRegion columnRegion)
            _colFormatRestoreData = sheet.Columns.SetFormatImpl(_cellFormat, columnRegion.Left, columnRegion.Right);
        else if (Region is RowRegion rowRegion)
            _rowFormatRestoreData = sheet.Rows.SetFormatImpl(_cellFormat, rowRegion.Top, rowRegion.Bottom);
        else
        {
            var region = sheet.Region.GetIntersection(Region);
            if (region != null && !_clearSurroundingBorders)
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

        // Define the bounds of the region
        int top = Region.Top;
        int bottom = Region.Bottom;
        int left = Region.Left;
        int right = Region.Right;

        // Create specific regions for the outer borders
        var topBoundary = new Region(top, top, left, right); // Topmost row of the region
        var bottomBoundary = new Region(bottom, bottom, left, right); // Bottommost row of the region
        var leftBoundary = new Region(top, bottom, left, left); // Leftmost column of the region
        var rightBoundary = new Region(top, bottom, right, right); // Rightmost column of the region

        // Create cell formats for each border using the existing _cellFormat.
        CellFormat? cfTop = null, cfBottom = null, cfLeft = null, cfRight = null;

        if (_cellFormat.BorderTop != null)
            cfTop = new CellFormat { BorderTop = _cellFormat.BorderTop.Clone() }; // Apply top border to bottom side of top row
        if (_cellFormat.BorderBottom != null)
            cfBottom = new CellFormat { BorderBottom = _cellFormat.BorderBottom.Clone() }; // Apply bottom border to top side of bottom row
        if (_cellFormat.BorderLeft != null)
            cfLeft = new CellFormat { BorderLeft = _cellFormat.BorderLeft.Clone() }; // Apply left border to right side of left column
        if (_cellFormat.BorderRight != null)
            cfRight = new CellFormat { BorderRight = _cellFormat.BorderRight.Clone() }; // Apply right border to left side of right column

        // Add border commands if applicable
        if (cfTop != null)
            _borderCommands.Add(new SetFormatCommand(topBoundary, cfTop, false));

        if (cfBottom != null)
            _borderCommands.Add(new SetFormatCommand(bottomBoundary, cfBottom, false));

        if (cfLeft != null)
            _borderCommands.Add(new SetFormatCommand(leftBoundary, cfLeft, false));

        if (cfRight != null)
            _borderCommands.Add(new SetFormatCommand(rightBoundary, cfRight, false));

        // Execute all border commands
        foreach (var cmd in _borderCommands)
            cmd.Execute(sheet);

        sheet.EndBatchUpdates();
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

        sheet.MarkDirty(Region);
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