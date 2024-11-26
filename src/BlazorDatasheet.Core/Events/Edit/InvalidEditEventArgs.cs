namespace BlazorDatasheet.Core.Events.Edit;

public class InvalidEditEventArgs
{
    /// <summary>
    /// The value of the editor when an invalid edit was made.
    /// </summary>
    public string EditValue { get; }

    /// <summary>
    /// The message explaining the invalid state.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Whether the event is handled. Set to true to avoid the default behaviour of an invalid edit (showing a dialog.)
    /// </summary>
    public bool Handled { get; set; }

    public InvalidEditEventArgs(string editValue, string message, bool handled)
    {
        EditValue = editValue;
        Message = message;
        Handled = handled;
    }
}