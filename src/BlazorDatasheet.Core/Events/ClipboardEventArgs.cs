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

    /// <summary>
    /// For a clipboardchange event: the clipboard was written by something other than a view of the workbook,
    /// e.g. a copy from elsewhere on the page. The content is not known.
    /// </summary>
    public bool Foreign { get; set; }

    /// <summary>
    /// Set by the browser when the content was too large to send with the event; the window event service
    /// fetches it before the event is handled.
    /// </summary>
    public int? PayloadId { get; set; }
}
