using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Specifies a range, which is a collection of regions in a sheet.
/// </summary>
public class BRange
{
    public readonly Sheet Sheet;
    protected List<IRegion> _regions;

    public IEnumerable<IRegion> Regions
    {
        get => _regions;
        set => _regions = value.ToList();
    }

    /// <summary>
    /// Return the positions present in the range. May be non-unique & include empty position
    /// depending on the regions present.
    /// </summary>
    public IEnumerable<CellPosition> Positions => _rangePositionEnumerator;

    private readonly RangePositionEnumerator _rangePositionEnumerator;

    /// <summary>
    /// Sets the value of all cells in the range.
    /// </summary>
    public object? Value
    {
        set => doSetValues(value);
    }

    internal BRange(Sheet sheet, IEnumerable<IRegion> regions)
    {
        Sheet = sheet;
        Regions = regions;
        _rangePositionEnumerator = new RangePositionEnumerator(this);
    }

    internal BRange(Sheet sheet, IRegion region) :
        this(sheet, new List<IRegion>() { region })
    {
    }

    /// <summary>
    /// Return all cells in this range.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCells()
    {
        return Positions.Select(x => Sheet.Cells.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Return all non-empty cells in this range.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetNonEmptyCells()
    {
        return this.GetNonEmptyPositions().Select(x => Sheet.Cells.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Return all positions of non-empty cells in the sheet.
    /// </summary>
    /// <returns>A collection of (row, column) positions of all non-empty cells.</returns>
    public IEnumerable<CellPosition> GetNonEmptyPositions()
    {
        return Regions.SelectMany(Sheet.Cells.GetNonEmptyCellPositions);
    }

    /// <summary>
    /// Sets all the cells in the range to the value specified.
    /// </summary>
    /// <param name="value"></param>
    private void doSetValues(object? value)
    {
        Sheet.Cells.SetValues(Positions.Select(x => (x.row, x.col, value)).ToList());
    }

    public void ClearCells()
    {
        Sheet.Cells.ClearCells(this);
    }

    public void AddRegion(IRegion region)
    {
        _regions.Add(region);
    }

    public void RemoveRegion(IRegion region)
    {
        _regions.Remove(region);
    }

    public BRange Clone()
    {
        return new BRange(this.Sheet, _regions.Select(x => x.Clone()));
    }
}