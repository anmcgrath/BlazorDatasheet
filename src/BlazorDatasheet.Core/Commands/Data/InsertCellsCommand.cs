using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Inserts blank cells over a region, shifting the cells after it right (<see cref="Axis.Col"/>) or down
/// (<see cref="Axis.Row"/>) within the rows (or columns) the region covers, as Excel's "Insert cells". Everything
/// keyed by position moves with the cells: values, formulas, formats, types, merges, validators, conditional formats,
/// metadata and tracked ranges. References lying entirely within the shifted band are adjusted, wherever the formula
/// is; references extending outside it are left alone, as in Excel.
/// <para>
/// Refused when a merged cell straddles the band, or when a value or formula would be pushed off the edge of the
/// sheet. Formats pushed off the edge are dropped.
/// </para>
/// <para>
/// Cells spanning every row (for <see cref="Axis.Col"/>) or every column (for <see cref="Axis.Row"/>) of the sheet
/// are whole columns (or rows), so this is then the same as inserting them.
/// </para>
/// </summary>
public class InsertCellsCommand : BaseCommand, IUndoableCommand
{
    private readonly IRegion _region;
    private readonly Axis _shiftAxis;

    private readonly List<(IRowColShiftingStore Store, object? RestoreData)> _storeRestoreData = new();
    private readonly List<(ConditionalFormatManager Manager, ConditionalFormatRestoreData Data)> _formatRestores = new();

    // Set when the region is whole columns (rows) and the command inserts them instead.
    private BaseCommand? _rowCol;

    /// <param name="region">The cells to insert. The cells there and after it move to make room.</param>
    /// <param name="shiftAxis"><see cref="Axis.Col"/> shifts cells right, <see cref="Axis.Row"/> shifts cells down.</param>
    public InsertCellsCommand(IRegion region, Axis shiftAxis)
    {
        _region = region.Clone();
        _shiftAxis = shiftAxis;
    }

    /// <summary>
    /// The command inserting whole columns (or rows) when the region spans the sheet across the shift axis,
    /// otherwise null.
    /// </summary>
    private BaseCommand? CreateRowColCommand(Sheet sheet)
    {
        var clipped = _region.GetIntersection(sheet.Region);
        if (clipped == null)
            return null;

        var fullBand = _shiftAxis == Axis.Col
            ? clipped.Top == 0 && clipped.Bottom == sheet.NumRows - 1
            : clipped.Left == 0 && clipped.Right == sheet.NumCols - 1;
        if (!fullBand)
            return null;

        var start = _shiftAxis == Axis.Col ? clipped.Left : clipped.Top;
        var count = _shiftAxis == Axis.Col ? clipped.Width : clipped.Height;
        return new InsertRowsColsCommand(start, count, _shiftAxis);
    }

    public override bool CanExecuteProtected(Sheet sheet)
    {
        if (CreateRowColCommand(sheet) is { } rowCol)
            return rowCol.CanExecuteProtected(sheet);

        var geometry = CellShiftGeometry.Create(sheet, _region, _shiftAxis);
        if (geometry == null)
            return true;

        var half = geometry.Value.HalfBand;
        return sheet.Protection.CanEdit(half) && sheet.Protection.CanFormat(half);
    }

    protected override bool CanExecuteCore(Sheet sheet)
    {
        if (CreateRowColCommand(sheet) is { } rowCol)
            return rowCol.CanExecuteUnprotected(sheet);

        if (CellShiftGeometry.Create(sheet, _region, _shiftAxis) is not { } geometry)
            return false;

        if (sheet.Cells.ContainsReadOnly(geometry.HalfBand))
            return false;

        if (!sheet.Cells.GetMerges(geometry.HalfBand).All(m => geometry.Band.Contains(m)))
            return false;

        // Excel refuses to push anything off the end of the sheet.
        var edge = _shiftAxis == Axis.Col
            ? new Region(geometry.Region.Top, geometry.Region.Bottom, sheet.NumCols - geometry.Count, sheet.NumCols - 1)
            : new Region(sheet.NumRows - geometry.Count, sheet.NumRows - 1, geometry.Region.Left, geometry.Region.Right);
        return !sheet.Cells.ContainsUserData(edge);
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        _rowCol = CreateRowColCommand(sheet);
        if (_rowCol != null)
            return _rowCol.ExecuteUnprotected(sheet);

        if (CellShiftGeometry.Create(sheet, _region, _shiftAxis) is not { } geometry)
            return false;

        using var updates = sheet.SuspendUpdates();

        _storeRestoreData.Clear();
        _formatRestores.Clear();
        foreach (var store in sheet.ShiftingStores)
            _storeRestoreData.Add((store, store.InsertCellsAt(geometry.Region, _shiftAxis)));

        foreach (var other in sheet.Workbook.Sheets.Where(other => !ReferenceEquals(other, sheet)))
            _formatRestores.Add((other.ConditionalFormats,
                other.ConditionalFormats.ShiftCells(geometry.Region, _shiftAxis, sheet.Name, true)));

        // Unlike inserting rows/columns, nothing raises an event that recalculates, and references extending outside
        // the band are not adjusted, so they now read different cells.
        sheet.FormulaEngine.CalculateSheet(true);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (_rowCol != null)
            return ((IUndoableCommand)_rowCol).Undo(sheet);

        if (CellShiftGeometry.Create(sheet, _region, _shiftAxis) is not { } geometry)
            return false;

        using var updates = sheet.SuspendUpdates();

        // The spills made by recalculating after the shift would otherwise be shifted back with the cells; the
        // restore puts back the ones from before, and the recalculation re-spills them.
        sheet.Cells.DeleteSpillsIntersecting(geometry.HalfBand);

        foreach (var (store, restoreData) in _storeRestoreData)
            store.Restore(restoreData);

        foreach (var (manager, data) in _formatRestores)
            manager.Restore(data);

        sheet.MarkDirty(geometry.HalfBand);
        sheet.FormulaEngine.CalculateSheet(true);
        return true;
    }

    public int RetainedCellCount => _rowCol is IUndoableCommand rowCol
        ? rowCol.RetainedCellCount
        : _storeRestoreData.Select(x => x.RestoreData).OfType<CellStoreRestoreData>().Sum(x => x.RetainedCellCount);
}
