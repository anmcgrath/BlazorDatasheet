using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Formats;

public abstract class ConditionalFormatAbstractBase : IEquatable<ConditionalFormatAbstractBase>
{
    /// <summary>
    /// The order that the conditional format should be applied.
    /// </summary>
    internal int Order { get; set; }

    /// <summary>
    /// List of ranges that the format includes
    /// </summary>
    internal readonly List<SheetRange> Ranges = new();

    /// <summary>
    /// Set true if, when one cell value is recalculated, the conditional format should be re-evaluated for all cells
    /// assigned to the conditional format.
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Whether the conditional format is True and should be run/applied.
    /// </summary>
    public Func<CellPosition, Sheet, bool>? Predicate { get; protected set; }

    /// <summary>
    /// Whether the conditional formats after this should not be applied if this is true.
    /// </summary>
    public bool StopIfTrue { get; set; }

    /// <summary>
    /// Set of cell positions in this format
    /// </summary>
    internal HashSet<CellPosition> Positions { get; } = new();

    public event EventHandler<ConditionalFormatRegionsChangedEventArgs> RegionsChanged;

    /// <summary>
    /// Get positions that only exist in both sets
    /// </summary>
    /// <param name="???"></param>
    /// <returns></returns>
    public IEnumerable<CellPosition> GetPositions(HashSet<CellPosition>? restrictedTo = null)
    {
        if (restrictedTo == null)
            return Positions.AsEnumerable();
        else
        {
            return Positions.Intersect(restrictedTo);
        }
    }

    internal void Add(SheetRange range)
    {
        Ranges.Add(range);
        foreach (var position in range.Positions)
        {
            Positions.Add(position);
        }

        emitRangeChanged(new List<IRegion>(), new[] { range.Region });
    }

    /// <summary>
    /// Shift regions based on the position of the row inserted
    /// </summary>
    /// <param name="indexAfter"></param>
    public void HandleRowInserted(int indexAfter) => HandleInsert(indexAfter, Axis.Row);

    /// <summary>
    /// Shift regions based on the position of the row removed
    /// </summary>
    /// <param name="index"></param>
    public void HandleRowRemoved(int index) => HandleRemove(index, Axis.Row);

    /// <summary>
    /// Shift regions based on the position of the col inserted
    /// </summary>
    /// <param name="indexAfter"></param>
    public void HandleColInserted(int indexAfter) => HandleInsert(indexAfter, Axis.Col);

    /// <summary>
    /// Shift regions based on the position of the col removed
    /// </summary>
    /// <param name="index"></param>
    public void HandleColRemoved(int index) => HandleRemove(index, Axis.Col);

    /// <summary>
    /// Handles the insertion of a row or column at a particular index
    /// </summary>
    /// <param name="indexAfter">The index that the row/column was inserted directly after</param>
    /// <param name="axis"></param>
    private void HandleInsert(int indexAfter, Axis axis)
    {
        this.updatePositions();
        // emitRangeChanged(regionsRemoved, regionsAdded);
    }

    /// <summary>
    /// Handles the removal of a row or column at a particular index
    /// </summary>
    /// <param name="index">The index that the row/column was removed at</param>
    /// <param name="axis"></param>
    public void HandleRemove(int index, Axis axis)
    {
        var regionsRemoved = new List<IRegion>();
        var regionsAdded = new List<IRegion>();

        this.updatePositions();
        emitRangeChanged(regionsRemoved, regionsAdded);
    }

    public IEnumerable<IReadOnlyCell> GetCells(Sheet sheet)
    {
        return Positions.Select(x => sheet.Cells.GetCell(x.row, x.col));
    }


    internal void Remove(SheetRange range)
    {
        Ranges.Remove(range);
        updatePositions();
        emitRangeChanged(new[] { range.Region }, new List<IRegion>());
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
    public abstract CellFormat? CalculateFormat(int row, int col, Sheet sheet);

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

    public bool Equals(ConditionalFormatAbstractBase? other)
    {
        if (other == null)
            return false;
        return other == this;
    }
}