namespace BlazorDatasheet.Core.Events;

public class CellMetaDataChangeEventArgs
{
    public CellMetaDataChangeEventArgs(int row, int col, string name, object? oldValue, object? newValue)
    {
        Name = name;
        OldValue = oldValue;
        NewValue = newValue;
        Row = row;
        Col = col;
    }

    public int Row { get; }
    public int Col { get; }
    public string Name { get; }
    public object? OldValue { get; }
    public object? NewValue { get; }
}