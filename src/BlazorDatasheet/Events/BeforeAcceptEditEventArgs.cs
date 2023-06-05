namespace BlazorDatasheet.Events;

public class BeforeAcceptEditEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public object? EditValue { get; }

    /// <summary>
    /// Determines whether the edit is accepted or not.
    /// </summary>
    public bool AcceptEdit { get; set; } = true;

    /// <summary>
    /// Whether the editor is cleared (regardless of whether edit is accepted).
    /// </summary>
    public bool EditorCleared { get; set; } = true;

    public BeforeAcceptEditEventArgs(int row, int col, object? EditValue)
    {
        Row = row;
        Col = col;
        this.EditValue = EditValue;
    }
}