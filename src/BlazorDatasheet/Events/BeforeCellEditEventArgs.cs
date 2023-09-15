namespace BlazorDatasheet.Events;

public class BeforeCellEditEventArgs
{
    public int Row { get; }
    public int Col { get; }
    /// <summary>
    ///  The value that will be passed to the editor. This can be modified.
    /// </summary>
    public string EditValue { get; set; }
    /// <summary>
    /// The type of editor. This can be modified.
    /// </summary>
    public string EditorType { get; set; }
    /// <summary>
    /// Whether or not to cancel the edit before it has begun. Set to true to cancel.
    /// </summary>
    public bool CancelEdit { get; set; }
}