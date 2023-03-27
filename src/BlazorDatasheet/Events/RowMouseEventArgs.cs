using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Events;

public class RowMouseEventArgs
{
    public RowMouseEventArgs(int rowIndex, MouseEventArgs mouseEventArgs)
    {
        RowIndex = rowIndex;
        MouseEventArgs = mouseEventArgs;
    }

    public int RowIndex { get; }
    public MouseEventArgs MouseEventArgs { get; }
}