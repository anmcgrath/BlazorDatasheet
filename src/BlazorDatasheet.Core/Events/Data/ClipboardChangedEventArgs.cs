using BlazorDatasheet.Core.Clipboard;

namespace BlazorDatasheet.Core.Events.Data;

public class ClipboardChangedEventArgs
{
    public ClipboardEntry? OldEntry { get; }
    public ClipboardEntry? NewEntry { get; }

    public ClipboardChangedEventArgs(ClipboardEntry? oldEntry, ClipboardEntry? newEntry)
    {
        OldEntry = oldEntry;
        NewEntry = newEntry;
    }
}
