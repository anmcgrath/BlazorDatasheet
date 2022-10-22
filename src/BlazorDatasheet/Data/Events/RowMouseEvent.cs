using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Data.Events;

public class RowMouseEvent
{
    public RowMouseEvent(int rowIndex, MouseEventArgs mouseEventArgs)
    {
        RowIndex = rowIndex;
        MouseEventArgs = mouseEventArgs;
    }

    public int RowIndex { get; }
    public MouseEventArgs MouseEventArgs { get; }
}