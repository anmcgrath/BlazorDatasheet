using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Events.Edit;

public class BeforeAcceptEditEventArgs
{
    public IReadOnlyCell Cell { get; }
    public object? EditValue { get; set; }

    /// <summary>
    /// Determines whether the edit is accepted or not.
    /// </summary>
    public bool AcceptEdit { get; set; } = true;

    /// <summary>
    /// Whether the editor is cleared (regardless of whether edit is accepted).
    /// </summary>
    public bool EditorCleared { get; set; } = true;

    public BeforeAcceptEditEventArgs(IReadOnlyCell cell, object? EditValue)
    {
        Cell = cell;
        this.EditValue = EditValue;
    }
}