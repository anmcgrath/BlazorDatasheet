using BlazorDatasheet.Model;
using Microsoft.AspNetCore.Components;

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

    [Parameter]
    public EditState EditState { get; set; }
}