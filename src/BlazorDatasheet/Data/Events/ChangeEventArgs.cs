using System.ComponentModel;

namespace BlazorDatasheet.Data.Events;

public class ChangeEventArgs : CancelEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public object OldValue { get; }
    public object NewValue { get; }

    public ChangeEventArgs(int row, int col, object oldValue, object newValue)
    {
        Row = row;
        Col = col;
        OldValue = oldValue;
        NewValue = newValue;
    }
}