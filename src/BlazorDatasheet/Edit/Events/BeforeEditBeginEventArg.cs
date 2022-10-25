namespace BlazorDatasheet.Edit.Events;

public class BeforeEditBeginEventArg
{
    public int Row { get; }
    public int Col { get; }
    public bool IsSoftEdit { get; }
    public EditEntryMode Mode { get; }
    public string Key { get; }

    public BeforeEditBeginEventArg(int row, int col, bool isSoftEdit, EditEntryMode mode, string key)
    {
        Row = row;
        Col = col;
        IsSoftEdit = isSoftEdit;
        Mode = mode;
        Key = key;
    }
}