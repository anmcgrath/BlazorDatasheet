namespace BlazorDatasheet.Core.Events.Visual;

public class SheetScreenUpdatingEventArgs
{
    public bool IsScreenUpdating { get; }
    public SheetScreenUpdatingEventArgs(bool isScreenUpdating)
    {
        IsScreenUpdating = isScreenUpdating;
    }
}