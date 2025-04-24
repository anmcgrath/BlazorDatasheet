using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Data;

public class WorkbookSheetRemovedEventArgs
{
    public Sheet Sheet { get; }

    public WorkbookSheetRemovedEventArgs(Sheet sheet)
    {
        Sheet = sheet;
    }
}