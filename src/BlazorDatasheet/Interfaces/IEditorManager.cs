using BlazorDatasheet.Edit;

namespace BlazorDatasheet.Interfaces;

public interface IEditorManager
{
    T GetEditedValue<T>();
    void SetEditedValue<T>(T value);
    AcceptEditResult AcceptEdit();
    CancelEditResult CancelEdit();
}