namespace BlazorDatasheet.Core.Data;

public class Workbook
{
    private List<Sheet> _sheets = new();
    public IReadOnlyCollection<Sheet> Sheets => _sheets;

    public Workbook(int nSheets = 0)
    {
    }
}