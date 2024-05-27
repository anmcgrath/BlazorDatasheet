namespace BlazorDatasheet.Menu;

public class MenuShownEventArgs
{
    public string MenuId { get; set; }
    public object? Context { get; set; }

    internal MenuShownEventArgs(string menuId, object? context)
    {
        MenuId = menuId;
        Context = context;
    }
}