using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.Events.Edit;

public class BeforeCellEditEventArgs
{
    public IReadOnlyCell Cell { get; }

    /// <summary>
    ///  The value that will be passed to the editor. This can be modified before editing starts.
    /// </summary>
    public string EditValue { get; set; }
    /// <summary>
    /// The type of editor. This can be modified.
    /// </summary>
    public string EditorType { get; set; }

    /// <summary>
    /// Whether or not to cancel the edit before it has begun. Set to true to cancel.
    /// </summary>
    public bool CancelEdit { get; set; } = false;

    public BeforeCellEditEventArgs(IReadOnlyCell cell, string? editValue, string editorType)
    {
        Cell = cell;
        EditValue = editValue;
        EditorType = editorType;
    }
}