using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Data;

/// <summary>
/// Provide convenient functions for setting/getting values in the sheet.
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
    public IEnumerable<(int row, int col)> Positions => _rangePositionEnumerator;

    private readonly RangePositionEnumerator _rangePositionEnumerator;

    /// <summary>
    /// Sets the value of all cells in the range.
    /// </summary>
    public object Value
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

    public IEnumerable<IReadOnlyCell> GetCells()
    {
        return Positions.Select(x => Sheet.GetCell(x.row, x.col));
    }

    public IEnumerable<IReadOnlyCell> GetNonEmptyCells()
    {
        var nonEmptyCells = new List<IReadOnlyCell>();
        foreach (var region in Regions)
        {
            nonEmptyCells.AddRange(Sheet.GetNonEmptyCellPositions(region)
                                      .Select(x => Sheet.GetCell(x.row, x.col)));
        }

        return nonEmptyCells;
    }

    /// <summary>
    /// Sets all the cells in the range to the value specified.
    /// </summary>
    /// <param name="value"></param>
    private void doSetValues(object value)
    {
        Sheet.SetCellValues(Positions.Select(x => new ValueChange(x.row, x.col, value)));
    }

    protected void AddRegion(IRegion region)
    {
        _regions.Add(region);
    }

    protected void RemoveRegion(IRegion region)
    {
        _regions.Remove(region);
    }

    public BRange Clone()
    {
        return new BRange(this.Sheet, _regions.Select(x => x.Clone()));
    }
}