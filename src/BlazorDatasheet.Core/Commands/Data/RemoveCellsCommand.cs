using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Removes the cells in a region, shifting the cells after it left (<see cref="Axis.Col"/>) or up
/// (<see cref="Axis.Row"/>) within the rows (or columns) the region covers, as Excel's "Delete cells". Everything
/// keyed by position moves with the cells: values, formulas, formats, types, merges, validators, conditional formats,
/// metadata and tracked ranges. References to removed cells become #REF!; other references lying entirely within the
/// shifted band are adjusted, and references extending outside it are left alone, as in Excel.
/// <para>Refused when a merged cell straddles the band or is only partly removed.</para>
/// </summary>
public class RemoveCellsCommand : BaseCommand, IUndoableCommand
{
    private readonly IRegion _region;
    private readonly Axis _shiftAxis;

    private readonly List<(IRowColShiftingStore Store, object? RestoreData)> _storeRestoreData = new();

    /// <param name="region">The cells to remove. The cells after it move to fill the gap.</param>
    /// <param name="shiftAxis"><see cref="Axis.Col"/> shifts cells left, <see cref="Axis.Row"/> shifts cells up.</param>
    public RemoveCellsCommand(IRegion region, Axis shiftAxis)
    {
        _region = region.Clone();
        _shiftAxis = shiftAxis;
    }

    public override bool CanExecuteProtected(Sheet sheet)
    {
        var geometry = CellShiftGeometry.Create(sheet, _region, _shiftAxis);
        if (geometry == null)
            return true;

        var half = geometry.Value.HalfBand;
        return sheet.Protection.CanEdit(half) && sheet.Protection.CanFormat(half);
    }

    protected override bool CanExecuteCore(Sheet sheet)
    {
        if (CellShiftGeometry.Create(sheet, _region, _shiftAxis) is not { } geometry)
            return false;

        if (sheet.Cells.ContainsReadOnly(geometry.HalfBand))
            return false;

        // Excel refuses to split a merged cell, whether across the band or across the removed cells.
        return sheet.Cells.GetMerges(geometry.HalfBand).All(m =>
            geometry.Band.Contains(m) && (!m.Intersects(geometry.Region) || geometry.Region.Contains(m)));
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        if (CellShiftGeometry.Create(sheet, _region, _shiftAxis) is not { } geometry)
            return false;

        using var updates = sheet.SuspendUpdates();

        _storeRestoreData.Clear();
        foreach (var store in sheet.ShiftingStores)
            _storeRestoreData.Add((store, store.RemoveCellsAt(geometry.Region, _shiftAxis)));

        // Unlike removing rows/columns, nothing raises an event that recalculates, and references extending outside
        // the band are not adjusted, so they now read different cells.
        sheet.FormulaEngine.CalculateSheet(true);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        if (CellShiftGeometry.Create(sheet, _region, _shiftAxis) is not { } geometry)
            return false;

        using var updates = sheet.SuspendUpdates();

        // The spills made by recalculating after the shift would otherwise be shifted back with the cells; the
        // restore puts back the ones from before, and the recalculation re-spills them.
        sheet.Cells.DeleteSpillsIntersecting(geometry.HalfBand);

        foreach (var (store, restoreData) in _storeRestoreData)
            store.Restore(restoreData);

        sheet.MarkDirty(geometry.HalfBand);
        sheet.FormulaEngine.CalculateSheet(true);
        return true;
    }

    public int RetainedCellCount =>
        _storeRestoreData.Select(x => x.RestoreData).OfType<CellStoreRestoreData>().Sum(x => x.RetainedCellCount);
}
