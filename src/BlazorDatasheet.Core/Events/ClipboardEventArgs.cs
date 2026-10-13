namespace BlazorDatasheet.Core.Events;

public class ClipboardEventArgs
{
    /// <summary>
    /// The text/plain content of the clipboard event.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The text/html content of the clipboard event, when the browser exposed any.
    /// </summary>
    public string? Html { get; set; }

    /// <summary>
    /// The event type: copy, cut or paste.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether Shift was held with the key that caused a paste (paste values only).
    /// </summary>
    public bool ShiftKey { get; set; }
}
