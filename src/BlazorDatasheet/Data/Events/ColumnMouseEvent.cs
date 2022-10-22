using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Data.Events;

public class ColumnMouseEvent
{
    public ColumnMouseEvent(int column, MouseEventArgs args)
    {
        Column = column;
        Args = args;
    }

    public int Column { get; }
    public MouseEventArgs Args { get; }
}