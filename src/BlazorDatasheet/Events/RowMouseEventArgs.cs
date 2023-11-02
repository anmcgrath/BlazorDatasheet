using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Events;

public class RowMouseEventArgs
{
    public RowMouseEventArgs(int rowIndex, MouseEventArgs args)
    {
        RowIndex = rowIndex;
        Args = args;
    }

    public int RowIndex { get; }
    public MouseEventArgs Args { get; }
}