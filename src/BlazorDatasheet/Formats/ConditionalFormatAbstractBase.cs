using BlazorDatasheet.Data;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats;

public abstract class ConditionalFormatAbstractBase
{
    /// <summary>
    /// The order that the conditional format should be applied.
    /// </summary>
    internal int Order { get; set; }

    /// <summary>
    /// List of regions that the format covers
    /// </summary>
    internal readonly List<IFixedSizeRegion> Regions = new();

    /// <summary>
    /// Set true if, when one cell value is recalculated, the conditional format should be re-evaluated for all cells
    /// assigned to the conditional format.
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Whether the conditional format is True and should be run/applied.
    /// </summary>
    public Func<(int row, int col), Sheet, bool>? Predicate { get; protected set; }

    /// <summary>
    /// Whether the conditional formats after this should not be applied if this is true.
    /// </summary>
    public bool StopIfTrue { get; set; }

    /// <summary>
    /// Set of cell positions in this format
    /// </summary>
    internal HashSet<(int row, int col)> Positions { get; private set; } = new();

    /// <summary>
    /// Get positions that only exist in both sets
    /// </summary>
    /// <param name="???"></param>
    /// <returns></returns>
    public IEnumerable<(int row, int col)> GetPositions(HashSet<(int row, int col)>? restrictedTo = null)
    {
        if (restrictedTo == null)
            return Positions.AsEnumerable();
        else
        {
            return Positions.Intersect(restrictedTo);
        }
    }

    internal void Add(IFixedSizeRegion region, Sheet sheet)
    {
        Regions.Add(region);
        var regionInsideSheet = region.GetIntersection(sheet.Region);
        foreach (var position in regionInsideSheet)
        {
            Positions.Add((position.Row, position.Col));
        }
    }

    public IEnumerable<Cell> GetCells(Sheet sheet)
    {
        return Positions.Select(x => sheet.GetCell(x.row, x.col));
    }


    internal void Remove(IFixedSizeRegion region)
    {
        // Easiest way for now is to remove all positions and recalculate
        Regions.Remove(region);
        Positions.Clear();
        foreach (var r in Regions)
        {
            foreach (var posn in r)
                Positions.Add((posn.Row, posn.Col));
        }
    }

    /// <summary>
    /// Returns the calculated format object
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="sheet"></param>
    /// <returns></returns>
    public abstract Format? CalculateFormat(int row, int col, Sheet sheet);

    /// <summary>
    /// Prepare for calculating one or many formats (useful for caching values).
    /// </summary>
    public virtual void Prepare(Sheet sheet)
    {
    }

    public virtual ConditionalFormatAbstractBase Clone()
    {
        return (ConditionalFormatAbstractBase)this.MemberwiseClone();
    }
}