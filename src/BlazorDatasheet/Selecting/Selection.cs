using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Selecting;

public class Selection
{
    public IRange Range { get; }
    public Sheet Sheet { get; }
    public SelectionMode Mode { get; }

    internal Selection(IRange range, Sheet sheet, SelectionMode mode)
    {
        Range = range;
        Sheet = sheet;
        Mode = mode;
    }
}