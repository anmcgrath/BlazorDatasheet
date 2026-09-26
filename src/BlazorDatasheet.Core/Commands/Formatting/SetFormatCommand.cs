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

    private readonly List<FormatChange> _changes = new();

    private record FormatChange(IRegion Region, CellStoreRestoreData? Cells, RowColFormatRestoreData? Tracks);

    /// <summary>
    /// Command to set the format of the range given. The cell format is merged into the existing format, so that
    /// only properties that are specifically defined in cellFormat are changed.
    /// </summary>
    /// <param name="region">The region to set the format for. Can be a cell, column or row range.</param>
    /// <param name="cellFormat">The new cell format.</param>
    /// <param name="clearSurroundingBorders">Whether to copy top/left borders onto the neighboring bottom/right edges</param>
    public SetFormatCommand(IRegion region, CellFormat cellFormat, bool clearSurroundingBorders = true)
    {
        _cellFormat = cellFormat;
        _clearSurroundingBorders = clearSurroundingBorders;
        Region = region.Clone();
    }

    public override bool CanExecuteProtected(Sheet sheet) => !_cellFormat.SpecifiesLock && sheet.Protection.CanFormat(Region);

    protected override bool ExecuteCore(Sheet sheet)
    {
        sheet.BatchUpdates();
        try
        {
            _changes.Clear();
            ApplyFormat(sheet, Region, _cellFormat);

            // The renderer owns shared edges on the cell above/left (bottom/right borders).
            if (_clearSurroundingBorders)
            {
                if (_cellFormat.BorderLeft is { } leftBorder && Region is not RowRegion)
                {
                    IRegion left = Region is ColumnRegion
                        ? new ColumnRegion(Region.Left - 1)
                        : new Region(Region.Top, Region.Bottom, Region.Left - 1, Region.Left - 1);
                    ApplyNeighborFormat(sheet, left, new CellFormat { BorderRight = leftBorder });
                }

                if (_cellFormat.BorderTop is { } topBorder && Region is not ColumnRegion)
                {
                    IRegion above = Region is RowRegion
                        ? new RowRegion(Region.Top - 1)
                        : new Region(Region.Top - 1, Region.Top - 1, Region.Left, Region.Right);
                    ApplyNeighborFormat(sheet, above, new CellFormat { BorderBottom = topBorder });
                }
            }

            return true;
        }
        finally
        {
            sheet.EndBatchUpdates();
        }
    }

    private void ApplyNeighborFormat(Sheet sheet, IRegion region, CellFormat format)
    {
        // Preserve the protection checks previously made by the nested formatting commands.
        if (sheet.Protection.CanFormat(region))
            ApplyFormat(sheet, region, format);
    }

    private void ApplyFormat(Sheet sheet, IRegion region, CellFormat format)
    {
        if (region is ColumnRegion)
            _changes.Add(new FormatChange(region, null,
                sheet.Columns.SetFormatImpl(format, region.Left, region.Right)));
        else if (region is RowRegion)
            _changes.Add(new FormatChange(region, null,
                sheet.Rows.SetFormatImpl(format, region.Top, region.Bottom)));
        else if (sheet.Region.GetIntersection(region) is { } bounded)
            _changes.Add(new FormatChange(bounded, sheet.Cells.MergeFormatImpl(bounded, format), null));
    }

    public bool Undo(Sheet sheet)
    {
        sheet.BatchUpdates();
        try
        {
            for (var i = _changes.Count - 1; i >= 0; i--)
            {
                var change = _changes[i];
                if (change.Tracks != null)
                    Restore(sheet, change.Tracks,
                        change.Region is ColumnRegion ? sheet.Columns.Formats : sheet.Rows.Formats);
                if (change.Cells != null)
                    sheet.Cells.Restore(change.Cells);
                sheet.MarkDirty(change.Region);
            }

            return true;
        }
        finally
        {
            sheet.EndBatchUpdates();
        }
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