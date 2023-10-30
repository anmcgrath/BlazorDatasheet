using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Specifies a range, which is a collection of regions in a sheet.
/// </summary>
public class BRange
{
    public readonly Sheet Sheet;
    protected List<IRegion> _regions = new();

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

    /// <summary>
    /// Create a range with the expression e.g A1, A:B, A1:A5, Allows multiple regions by seperating with a ',' etc.
    /// </summary>
    /// <param name="rangeExpression"></param>
    internal BRange(Sheet sheet, string rangeExpression)
    {
        Sheet = sheet;
        _rangePositionEnumerator = new RangePositionEnumerator(this);

        if (string.IsNullOrEmpty(rangeExpression))
            return;

        foreach (var split in rangeExpression.Split(","))
        {
            var region = Region.FromString(split);
            if (region != null)
                _regions.Add(region);
        }
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
    /// Return all non-empty cells (in terms of Value) in this range.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetNonEmptyCells()
    {
        return this.GetNonEmptyPositions().Select(x => Sheet.Cells.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Return all positions of non-empty cells (in terms of Value) in the sheet.
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

    public void Clear()
    {
        Sheet.Cells.ClearCells(this);
    }

    internal void AddRegion(IRegion region)
    {
        _regions.Add(region);
    }

    internal void RemoveRegion(IRegion region)
    {
        _regions.Remove(region);
    }

    internal BRange Clone()
    {
        return new BRange(this.Sheet, _regions.Select(x => x.Clone()));
    }
}