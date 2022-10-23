namespace BlazorDatasheet.Data;

public class SheetRange
{
    private readonly Sheet _sheet;
    public IReadOnlyCollection<IRegion> Regions { get; }
    internal SheetRange(Sheet sheet, List<IRegion> regions)
    {
        _sheet = sheet;
        Regions = regions;
    }
}