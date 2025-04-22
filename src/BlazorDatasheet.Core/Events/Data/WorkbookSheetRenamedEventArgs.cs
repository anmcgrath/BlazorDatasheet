using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events.Data;

public class WorkbookSheetRenamedEventArgs
{
    public Sheet Sheet { get; }
    public string OldName { get; }
    public string NewName { get; }

    public WorkbookSheetRenamedEventArgs(Sheet sheet, string oldName, string newName)
    {
        Sheet = sheet;
        OldName = oldName;
        NewName = newName;
    }
}