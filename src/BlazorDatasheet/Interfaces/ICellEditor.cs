using BlazorDatasheet.Edit;

namespace BlazorDatasheet.Interfaces;

public interface ICellEditor
{
    EditorManager EditorManager { get; set; }
    bool CanAcceptEdit();
    bool CanCancelEdit();
    void BeginEdit(EditEntryMode entryMode, IReadOnlyCell cell, string key);
    bool HandleKey(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey);
}