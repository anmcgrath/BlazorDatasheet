using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Clipboard;

/// <summary>
/// The cells currently on the workbook's clipboard. The entry refers to the live source cells; what gets
/// pasted is whatever they hold at paste time.
/// </summary>
public sealed class ClipboardEntry
{
    /// <summary>
    /// Identifies this entry. It is written into the system clipboard so that a paste can tell our own
    /// content from external content.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// The source cells, clipped to their sheet.
    /// </summary>
    public SheetRange Source { get; }

    /// <summary>
    /// The sheet the source cells are on.
    /// </summary>
    public Sheet Sheet => Source.Sheet;

    public ClipboardMode Mode { get; }

    internal ClipboardEntry(Guid id, SheetRange source, ClipboardMode mode)
    {
        Id = id;
        Source = source;
        Mode = mode;
    }
}
