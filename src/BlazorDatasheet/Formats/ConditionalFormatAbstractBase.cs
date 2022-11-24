using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;

namespace BlazorDatasheet.Formats;

public abstract class ConditionalFormatAbstractBase
{
    /// <summary>
    /// The order that the conditional format should be applied.
    /// </summary>
    internal int Order { get; set; }

    /// <summary>
    /// List of ranges that the format includes
    /// </summary>
    internal readonly List<BRange> Ranges = new();

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

    public event EventHandler<ConditionalFormatRegionsChangedEventArgs> RegionsChanged;

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

    internal void Add(BRange range)
    {
        Ranges.Add(range);
        foreach (var position in range.Positions)
        {
            Positions.Add(position);
        }

        emitRangeChanged(new List<IRegion>(), range.Regions.ToList());
    }

    /// <summary>
    /// Shift regions based on the position of the row inserted
    /// </summary>
    /// <param name="index"></param>
    public void HandleRowInsertedAfter(int index)
    {
        var regionsRemoved = new List<IRegion>();
        var regionsAdded = new List<IRegion>();

        foreach (var range in Ranges)
        {
            foreach (var region in range.Regions.ToArray())
            {
                if (!(region.Top >= index))
                    continue;

                // Add and remove the region after shifting
                range.RemoveRegion(region);
                var shiftedRegion = region.Copy();
                shiftedRegion.Shift(1, 0);
                range.AddRegion(shiftedRegion);
                regionsRemoved.Add(region);
                regionsAdded.Add(shiftedRegion);
            }
        }

        this.updatePositions();
        emitRangeChanged(regionsRemoved, regionsAdded);
    }

    /// <summary>
    /// Shift regions based on the position of the row removed
    /// </summary>
    /// <param name="index"></param>
    public void HandleRowRemoved(int index)
    {
        var regionsRemoved = new List<IRegion>();
        var regionsAdded = new List<IRegion>();

        foreach (var range in Ranges)
        {
            foreach (var region in range.Regions.ToArray())
            {
                if (!(region.Top > index))
                    continue;

                // Add and remove the region after shifting
                range.RemoveRegion(region);
                var shiftedRegion = region.Copy();
                shiftedRegion.Shift(-1, 0);
                regionsRemoved.Add(region);
                range.AddRegion(shiftedRegion);
                regionsAdded.Add(shiftedRegion);
            }
        }

        this.updatePositions();
        emitRangeChanged(regionsRemoved, regionsAdded);
    }

    public IEnumerable<IReadOnlyCell> GetCells(Sheet sheet)
    {
        return Positions.Select(x => sheet.GetCell(x.row, x.col));
    }


    internal void Remove(BRange range)
    {
        Ranges.Remove(range);
        updatePositions();
        emitRangeChanged(range.Regions.ToList(), new List<IRegion>());
    }

    private void updatePositions()
    {
        Positions.Clear();
        foreach (var r in Ranges)
        {
            foreach (var posn in r.Positions)
                Positions.Add(posn);
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

    private void emitRangeChanged(IEnumerable<IRegion> regionsRemoved, IEnumerable<IRegion> regionsAdded)
    {
        var args = new ConditionalFormatRegionsChangedEventArgs(regionsRemoved, regionsAdded);
        RegionsChanged?.Invoke(this, args);
    }

    public virtual ConditionalFormatAbstractBase Clone()
    {
        return (ConditionalFormatAbstractBase)this.MemberwiseClone();
    }
}