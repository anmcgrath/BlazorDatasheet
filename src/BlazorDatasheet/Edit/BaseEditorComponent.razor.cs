using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Model;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Edit;

public abstract partial class BaseEditorComponent
{
    [Parameter] public EditState EditState { get; set; }

    public abstract bool CanAcceptEdit();
    public abstract bool CanCancelEdit();


    public string EditString
    {
        get { return EditState.EditString; }
        set { EditState.EditString = value; }
    }

    public bool IsSoftEdit
    {
        get { return EditState.IsSoftEdit; }
        set { EditState.IsSoftEdit = value; }
    }

    public abstract void BeginEdit(EditEntryMode entryMode, IWriteableCell cell, string key);

    public virtual bool HandleKey(string key)
    {
        return false;
    }

    protected bool? CancelEdit()
    {
        return EditState?.OnCancelEdit?.Invoke();
    }

    protected bool? AcceptEdit()
    {
        return EditState?.OnAcceptEdit?.Invoke();
    }
}