using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Data;

public class WorkbookSheetAddedEventArgs
{
    public Sheet Sheet { get; }

    public WorkbookSheetAddedEventArgs(Sheet sheet)
    {
        Sheet = sheet;
    }
}