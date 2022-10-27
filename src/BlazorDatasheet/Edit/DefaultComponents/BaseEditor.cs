using BlazorDatasheet.Interfaces;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Edit.DefaultComponents;

public abstract class BaseEditor : ComponentBase, ICellEditor
{
    public event EventHandler? RequestCancelEdit;
    public event EventHandler? RequestAcceptEdit;

    protected void CancelEdit() => RequestCancelEdit?.Invoke(this, EventArgs.Empty);
    protected void AcceptEdit() => RequestAcceptEdit?.Invoke(this, EventArgs.Empty);
    
    private bool FocusRequested { get; set; }

    /// <summary>
    /// If this is linked to the editor's input reference then the base editor will handle focusing.
    /// </summary>
    protected ElementReference? InputRef;

    public virtual void BeforeEdit(IReadOnlyCell cell)
    {
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (FocusRequested && InputRef != null)
        {
            await InputRef.Value.FocusAsync();
            FocusRequested = false;
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    public async Task Focus()
    {
        FocusRequested = true;
    }

    public virtual bool CanAcceptEdit() => true;

    public virtual bool CanCancelEdit() => true;

    public virtual object? GetValue()
    {
        return default;
    }

    public abstract void BeginEdit(EditEntryMode entryMode, IReadOnlyCell cell, string key);

    public virtual bool HandleKey(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey)
    {
        return false;
    }

    public void Render() => StateHasChanged();
}