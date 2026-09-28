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
    /// <param name="clearSurroundingBorders">Whether to clear the borders that neighbouring cells hold on the edges
    /// this format sets or clears a border on, so that the border set last is the one drawn.</param>
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

            // An edge is shared by two cells and either may hold its border. Clearing the
            // neighbour's side of every edge this format speaks for leaves the edge with one owner,
            // so the border set last is the one drawn and clearing a border removes it.
            if (_clearSurroundingBorders)
            {
                ClearOppositeSide(sheet, nameof(CellFormat.BorderLeft), nameof(CellFormat.BorderRight), 0, -1);
                ClearOppositeSide(sheet, nameof(CellFormat.BorderRight), nameof(CellFormat.BorderLeft), 0, 1);
                ClearOppositeSide(sheet, nameof(CellFormat.BorderTop), nameof(CellFormat.BorderBottom), -1, 0);
                ClearOppositeSide(sheet, nameof(CellFormat.BorderBottom), nameof(CellFormat.BorderTop), 1, 0);
            }

            return true;
        }
        finally
        {
            sheet.EndBatchUpdates();
        }
    }

    private void ClearOppositeSide(Sheet sheet, string side, string opposite, int dRow, int dCol)
    {
        if (!_cellFormat.Specifies(side))
            return;

        // When the format sets the opposite side as well, the edges inside the region are given
        // the same border from both sides and only the edge around the region has a neighbour to clear.
        var outsideOnly = _cellFormat.Specifies(opposite);
        var neighbours = GetNeighbours(sheet, dRow, dCol, outsideOnly);
        // Most edges have no border on the neighbour's side, and clearing one that is not there
        // would only break the stored formats into more pieces.
        if (neighbours != null && HoldsBorder(sheet, neighbours, opposite))
            ApplyNeighborFormat(sheet, neighbours,
                new CellFormat(new Dictionary<string, object?> { { opposite, null } }));
    }

    /// <summary>
    /// Whether any cell in the region is given a border on the side, by its own format or by the
    /// format of its row or column.
    /// </summary>
    private static bool HoldsBorder(Sheet sheet, IRegion region, string side)
    {
        foreach (var interval in sheet.Columns.Formats.GetIntervals(region.Left, region.Right))
            if (interval.Data.GetBorder(side) != null)
                return true;

        foreach (var interval in sheet.Rows.Formats.GetIntervals(region.Top, region.Bottom))
            if (interval.Data.GetBorder(side) != null)
                return true;

        foreach (var data in sheet.Cells.GetFormatData(region))
            if (data.Data.GetBorder(side) != null)
                return true;

        return false;
    }

    /// <summary>
    /// The cells on the other side of the region's edges in the direction given: the region moved
    /// by one row or column, or only the row or column just outside it.
    /// </summary>
    private IRegion? GetNeighbours(Sheet sheet, int dRow, int dCol, bool outsideOnly)
    {
        if (sheet.NumRows == 0 || sheet.NumCols == 0)
            return null;

        var top = Math.Max(Region.Top, 0);
        var left = Math.Max(Region.Left, 0);
        var bottom = Math.Min(Region.Bottom, sheet.NumRows - 1);
        var right = Math.Min(Region.Right, sheet.NumCols - 1);
        if (top > bottom || left > right)
            return null;

        if (outsideOnly)
        {
            if (dCol != 0)
                left = right = dCol < 0 ? left - 1 : right + 1;
            if (dRow != 0)
                top = bottom = dRow < 0 ? top - 1 : bottom + 1;
        }
        else
        {
            left += dCol;
            right += dCol;
            top += dRow;
            bottom += dRow;
        }

        top = Math.Max(top, 0);
        left = Math.Max(left, 0);
        bottom = Math.Min(bottom, sheet.NumRows - 1);
        right = Math.Min(right, sheet.NumCols - 1);
        if (top > bottom || left > right)
            return null;

        // a row or column stays one only while it is moved along its own axis; moved across it,
        // the cells at the sheet's edge have no neighbour and keep their border.
        if (Region is ColumnRegion && dCol != 0)
            return new ColumnRegion(left, right);
        if (Region is RowRegion && dRow != 0)
            return new RowRegion(top, bottom);
        return new Region(top, bottom, left, right);
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