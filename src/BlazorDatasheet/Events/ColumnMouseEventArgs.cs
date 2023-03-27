using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Events;

public class ColumnMouseEventArgs
{
    public ColumnMouseEventArgs(int column, MouseEventArgs args)
    {
        Column = column;
        Args = args;
    }

    public int Column { get; }
    public MouseEventArgs Args { get; }
}