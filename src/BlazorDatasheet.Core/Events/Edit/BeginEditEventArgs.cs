using BlazorDatasheet.Core.Edit;

namespace BlazorDatasheet.Core.Events.Edit;

public class BeginEditEventArgs
{
    public int Row { get; }
    public int Col { get; }
    public bool IsSoftEdit { get; }
    public EditEntryMode Mode { get; }
    public string EntryChar { get; }

    public BeginEditEventArgs(int row, int col, bool isSoftEdit, EditEntryMode mode, string entryChar)
    {
        Row = row;
        Col = col;
        IsSoftEdit = isSoftEdit;
        Mode = mode;
        EntryChar = entryChar;
    }
}