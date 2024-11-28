using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Edit;

public abstract class BaseEditor : SheetComponentBase, ICellEditor
{
    [Parameter] public EventCallback<string> OnValueChanged { get; set; }

    private string _currentValue = string.Empty;

    public string CurrentValue
    {
        get => _currentValue;
        protected set
        {
            var changed = _currentValue != value;
            _currentValue = value;
            if (changed)
            {
                OnValueChanged.InvokeAsync(value);
                StateHasChanged();
            }
        }
    }

    public event EventHandler? RequestCancelEdit;
    public event EventHandler? RequestAcceptEdit;

    protected void CancelEdit() => RequestCancelEdit?.Invoke(this, EventArgs.Empty);
    protected void AcceptEdit() => RequestAcceptEdit?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// If this is linked to the editor's input reference then the base editor will handle focusing.
    /// </summary>
    public ElementReference InputRef = new ElementReference();

    /// <summary>
    /// Style to apply to the editor such as background and font.
    /// </summary>
    [Parameter, EditorRequired]
    public required string Style { get; set; }

    /// <summary>
    /// The cell width in px
    /// </summary>
    [Parameter, EditorRequired] 
    public required double CellWidth { get; set; }

    /// <summary>
    /// The cell height in px
    /// </summary>
    [Parameter, EditorRequired] 
    public required double CellHeight { get; set; }

    public virtual void BeforeEdit(IReadOnlyCell cell, Sheet sheet)
    {
    }

    public abstract void BeginEdit(EditEntryMode entryMode, string? editValue, string key);

    public virtual bool HandleKey(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey)
    {
        return false;
    }

    public virtual void HandleEditValueChange(string? s)
    {
        if (s != _currentValue)
        {
            _currentValue = s;
            StateHasChanged();
        }
    }
}