namespace BlazorDatasheet.Model;

public class Selection
{
    public Range Range { get; }
    public Sheet Sheet { get; }
    public SelectionMode Mode { get; }

    internal Selection(Range range, Sheet sheet, SelectionMode mode)
    {
        Range = range;
        Sheet = sheet;
        Mode = mode;
    }

    /// <summary>
    /// Moves the selection end (ColEnd, RowEnd) to the row & col specified
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal void ExtendTo(int row, int col)
    {
        this.Range.RowEnd = row;
        this.Range.ColEnd = col;
        this.Range.Constrain(Sheet.Range);
    }
}