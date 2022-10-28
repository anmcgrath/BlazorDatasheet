using BlazorDatasheet.Data;
using BlazorDatasheet.Edit;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Interfaces;

public interface ICellEditor
{
    /// <summary>
    /// Call this event when the editor wants to cancel edit
    /// </summary>
    event EventHandler RequestCancelEdit;

    /// <summary>
    /// Call this event when the editor wants to accept edit
    /// </summary>
    event EventHandler RequestAcceptEdit;
    void BeforeEdit(IReadOnlyCell cell);
    bool CanAcceptEdit();
    bool CanCancelEdit();
    object? GetValue();
    void BeginEdit(EditEntryMode entryMode, IReadOnlyCell cell, string key);
    bool HandleKey(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey);
    Task Focus();
}