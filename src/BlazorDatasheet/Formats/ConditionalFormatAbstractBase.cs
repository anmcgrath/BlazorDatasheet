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
    /// List of ranges that the format covers
    /// </summary>
    internal List<IFixedSizeRange> Ranges { get; private set; } = new();

    /// <summary>
    /// Set true if, when one cell value is recalculated, the conditional format should be re-evaluated for all cells
    /// assigned to the conditional format.
    /// </summary>
    public bool IsShared { get; set; }

    /// <summary>
    /// Whether the conditional format is True and should be run/applied.
    /// </summary>
    public Func<CellPosition, Sheet, bool> IsTrue { get; protected set; }

    /// <summary>
    /// Whether the conditional formats after this should not be applied if this is true.
    /// </summary>
    public bool StopIfTrue { get; set; }

    /// <summary>
    /// Set of cell positions in this format
    /// </summary>
    private HashSet<(int row, int col)> Positions { get; set; } = new();

    internal void Add(IFixedSizeRange range)
    {
        Ranges.Add(range);
        foreach (var position in range)
        {
            Positions.Add((position.Row, position.Col));
        }
    }

    public IEnumerable<Cell> GetCells(Sheet sheet)
    {
        return Positions.Select(x => sheet.GetCell(x.row, x.col));
    }


    internal void Remove(IFixedSizeRange range)
    {
        // Easiest way for now is to remove all positions and recalculate
        Ranges.Remove(range);
        Positions.Clear();
        foreach (var r in Ranges)
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
}