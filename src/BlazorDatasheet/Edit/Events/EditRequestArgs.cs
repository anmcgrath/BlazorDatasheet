namespace BlazorDatasheet.Edit.Events;

public class EditRequestArgs
{
    public int Row { get; }
    public int Col { get; }
    public bool IsSoftEdit { get; }
    public EditEntryMode EntryMode { get; }

    public EditRequestArgs(int row, int col, bool isSoftEdit, EditEntryMode entryMode)
    {
        Row = row;
        Col = col;
        IsSoftEdit = isSoftEdit;
        EntryMode = entryMode;
    }
}