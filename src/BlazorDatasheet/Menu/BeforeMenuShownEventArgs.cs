using System.ComponentModel;

namespace BlazorDatasheet.Menu;

public class BeforeMenuShownEventArgs : CancelEventArgs
{
    public string MenuId { get; set; }
    public object? Context { get; set; }

    internal BeforeMenuShownEventArgs(string menuId, object? context)
    {
        MenuId = menuId;
        Context = context;
    }
}