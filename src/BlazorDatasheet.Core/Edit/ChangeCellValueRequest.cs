namespace BlazorDatasheet.Core.Edit;

/// <summary>
/// Used when a renderer requests that the cell's value is changed without creating an editor.
/// </summary>
public class ChangeCellValueRequest
{
    public ChangeCellValueRequest(int row, int col, object newValue)
    {
        Row = row;
        Col = col;
        NewValue = newValue;
    }

    public int Row { get; }
    public int Col { get; }
    public object NewValue { get; set; }
}