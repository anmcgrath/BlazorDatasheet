using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Data;

/// <summary>
/// Raised before the clipboard is pasted. Set <see cref="Cancel"/> to stop the paste, or change
/// <see cref="Mode"/> to alter what is pasted.
/// </summary>
public class BeforePasteEventArgs
{
    /// <summary>
    /// The sheet being pasted into.
    /// </summary>
    public Sheet TargetSheet { get; }

    /// <summary>
    /// The selection being pasted into. The pasted block starts at its top-left and may tile across it.
    /// </summary>
    public IRegion TargetRegion { get; }

    public PasteMode Mode { get; set; }

    /// <summary>
    /// The workbook clipboard entry being pasted, or null when the content came from outside the workbook.
    /// </summary>
    public ClipboardEntry? Entry { get; }

    /// <summary>
    /// The external content being pasted, or null for a paste of the workbook clipboard entry.
    /// </summary>
    public ClipboardSnapshot? Snapshot { get; init; }

    public bool IsInternal => Entry != null;

    public bool Cancel { get; set; }

    public BeforePasteEventArgs(Sheet targetSheet, IRegion targetRegion, PasteMode mode, ClipboardEntry? entry)
    {
        TargetSheet = targetSheet;
        TargetRegion = targetRegion;
        Mode = mode;
        Entry = entry;
    }
}
