namespace BlazorDatasheet.Core.Edit;

/// <summary>
/// Used when a renderer requests that the cell should begin being edited
/// </summary>
public class CellEditRequest
{
    /// <summary>
    /// The cell's row
    /// </summary>
    public int Row { get; }
    /// <summary>
    /// The cell's column
    /// </summary>
    public int Col { get; }
    /// <summary>
    /// Whether the edit is a "soft edit"
    /// </summary>
    public bool IsSoftEdit { get; }
    /// <summary>
    /// The mode of entry for the editor
    /// </summary>
    public EditEntryMode EntryMode { get; }

    public CellEditRequest(int row, int col, bool isSoftEdit, EditEntryMode entryMode)
    {
        Row = row;
        Col = col;
        IsSoftEdit = isSoftEdit;
        EntryMode = entryMode;
    }
}