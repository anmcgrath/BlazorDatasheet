using BlazorDatasheet.Model;

namespace BlazorDatasheet.Edit;

public interface ICellEditor
{
    public void BeginEdit(EditEntryMode entryMode, Cell cell, string key);

    /// <summary>
    /// Instruct the editor to handle a key press in the window
    /// </summary>
    /// <param name="key"></param>
    /// <returns>Whether the event should be cancelled</returns>
    public bool HandleKey(string key);

    public Func<bool> OnAcceptEdit { get; set; }
    public Func<bool> OnCancelEdit { get; set; }
    public bool CanCancelEdit { get; }
    public bool CanAcceptEdit { get; }
    public bool IsSoftEdit { get; }
    public string EditString { get; }
}