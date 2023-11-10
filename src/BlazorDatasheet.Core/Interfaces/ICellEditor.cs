using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;

namespace BlazorDatasheet.Core.Interfaces;

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

    void BeforeEdit(IReadOnlyCell cell, Sheet sheet);
    void BeginEdit(EditEntryMode entryMode, string? editValue, string key);
    bool HandleKey(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey);
}