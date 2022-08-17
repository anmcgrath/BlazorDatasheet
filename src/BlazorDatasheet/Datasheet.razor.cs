using BlazorDatasheet.Edit;
using BlazorDatasheet.Model;
using BlazorDatasheet.Render;
using BlazorDatasheet.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet;

public partial class Datasheet : IHandleEvent
{
    [Parameter] public Sheet? Sheet { get; set; }

    private ICellEditor ActiveEditorReference { get; set; }
    private bool IsDataSheetActive { get; set; }
    private CellPosition? EditPosition { get; set; }
    private bool IsEditing => EditPosition != null;
    private bool IsMouseInsideSheet { get; set; }
    private ElementReference ActiveCellInputReference;
    private Queue<Action> QueuedActions { get; set; } = new Queue<Action>();
    private Dictionary<string, Type> RenderComponentTypes { get; set; }

    protected override void OnInitialized()
    {
        RenderComponentTypes = new Dictionary<string, Type>();
        RenderComponentTypes.Add("text", typeof(TextRenderer));
        RenderComponentTypes.Add("number", typeof(NumberRenderer));
        RenderComponentTypes.Add("boolean", typeof(BoolRenderer));
        base.OnInitialized();
    }

    private Type getCellRendererType(string type)
    {
        if (RenderComponentTypes.ContainsKey(type))
            return RenderComponentTypes[type];

        return typeof(TextRenderer);
    }

    private Dictionary<string, object> getCellRendererParameters(Cell cell)
    {
        return new Dictionary<string, object>()
        {
            { "Cell", cell },
        };
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AddWindowEventsAsync();
        }

        while (QueuedActions.Any())
        {
            var action = QueuedActions.Dequeue();
            action.Invoke();
        }
    }

    private async Task AddWindowEventsAsync()
    {
        await _WindowEventService.Init();
        _WindowEventService.OnKeyDown += HandleWindowKeyDown;
        _WindowEventService.OnMouseDown += HandleWindowMouseDown;
    }

    private void HandleCellMouseUp(int row, int col, MouseEventArgs e)
    {
        if (Sheet.IsSelecting)
        {
            Sheet?.EndSelecting();
            StateHasChanged();
        }
    }

    private void HandleCellMouseDown(int row, int col, MouseEventArgs e)
    {
        if (IsEditing && !EditPosition.Equals(row, col))
            AcceptEdit();

        if (e.ShiftKey)
            Sheet?.ExtendSelection(row, col);
        else
            Sheet?.BeginSelecting(row, col, !e.MetaKey, SelectionMode.Cell);

        StateHasChanged();
    }

    private void HandleColumnHeaderMouseDown(int col, MouseEventArgs e)
    {
        AcceptEdit();

        if (e.ShiftKey)
            Sheet?.ExtendSelection(Sheet.NumRows, col);
        else
        {
            Sheet?.BeginSelecting(0, col, !e.MetaKey, SelectionMode.Column);
            Sheet?.UpdateSelectingEndPosition(Sheet.NumRows, col);
        }

        StateHasChanged();
    }

    private void HandleRowHeaderMouseDown(int row, MouseEventArgs e)
    {
        AcceptEdit();

        if (e.ShiftKey)
            Sheet?.ExtendSelection(row, Sheet.NumCols);
        else
        {
            Sheet?.BeginSelecting(row, 0, !e.MetaKey, SelectionMode.Row);
            Sheet?.UpdateSelectingEndPosition(row, Sheet.NumCols);
        }

        StateHasChanged();
    }

    private void HandleCellDoubleClick(int row, int col, MouseEventArgs e)
    {
        BeginEdit(row, col, softEdit: false, EditEntryMode.Mouse);
        StateHasChanged();
    }

    private void BeginEdit(int row, int col, bool softEdit, EditEntryMode mode, string entryChar = "")
    {
        var cell = Sheet?.GetCell(row, col);
        if (cell == null)
            return;

        EditPosition = new CellPosition() { Row = row, Col = col };

        // Do this after the next render because the EditComponent doesn't exist until then
        NextTick(() =>
        {
            ActiveEditorReference.OnAcceptEdit = AcceptEdit;
            ActiveEditorReference.OnCancelEdit = CancelEdit;
            ActiveEditorReference?.BeginEdit(mode, cell, entryChar);
        });
    }

    private bool AcceptEdit()
    {
        return AcceptEdit(0, 0);
    }

    /// <summary>
    /// Accepts the current edit, returning whether the edit was successful
    /// </summary>
    /// <param name="dRow"></param>
    /// <param name="dCol"></param>
    /// <returns></returns>
    private bool AcceptEdit(int dRow, int dCol)
    {
        if (!IsEditing)
            return false;

        if (ActiveEditorReference.CanAcceptEdit)
        {
            var cell = Sheet.GetCell(EditPosition.Row, EditPosition.Col);
            cell.Value = ActiveEditorReference.EditString;

            // Finish the edit
            EditPosition = null;

            Sheet.MoveSelection(dRow, dCol);
            StateHasChanged();

            return true;
        }

        return false;
    }

    /// <summary>
    /// Cancels the current edit, returning whether the edit was successful
    /// </summary>
    /// <returns></returns>
    private bool CancelEdit()
    {
        if (!IsEditing)
            return false;

        if (ActiveEditorReference.CanCancelEdit)
        {
            // Finish the edit
            EditPosition = null;
            StateHasChanged();
            return true;
        }

        return false;
    }

    private void HandleCellMouseOver(int row, int col, MouseEventArgs e)
    {
        if (Sheet?.IsSelecting == true)
        {
            if (Sheet.SelectionMode == SelectionMode.Cell)
                Sheet.UpdateSelectingEndPosition(row, col);
            else if (Sheet.SelectionMode == SelectionMode.Column)
                Sheet.UpdateSelectingEndPosition(Sheet.NumRows, col);
            else if (Sheet.SelectionMode == SelectionMode.Row)
                Sheet.UpdateSelectingEndPosition(row, Sheet.NumCols);
            StateHasChanged();
        }
    }

    private bool HandleWindowMouseDown(MouseEventArgs e)
    {
        bool changed = IsDataSheetActive != IsMouseInsideSheet;
        IsDataSheetActive = IsMouseInsideSheet;

        if (!IsDataSheetActive) // if it is outside
        {
            AcceptEdit();
            changed = true;
        }

        if (changed)
            StateHasChanged();

        return false;
    }

    private bool HandleWindowKeyDown(KeyboardEventArgs e)
    {
        if (!IsDataSheetActive)
            return false;

        if (IsEditing)
        {
            var handled = ActiveEditorReference.HandleKey(e.Key);
            if (handled)
                return true;
        }
        
        if (e.Key == "Escape")
        {
            return CancelEdit();
        }

        if (e.Key == "Enter")
        {
            if (AcceptEdit())
            {
                Sheet?.MoveSelection(1, 0);
                StateHasChanged();
                return true;
            }
        }

        if (KeyUtil.IsArrowKey(e.Key))
        {
            var direction = KeyUtil.GetKeyMovementDirection(e.Key);
            if (!IsEditing)
            {
                Sheet?.MoveSelection(direction.Item1, direction.Item2);
                StateHasChanged();
                return true;
            }
            else if (ActiveEditorReference.IsSoftEdit && AcceptEdit(direction.Item1, direction.Item2))
            {
                return true;
            }
        }

        if (e.Key.Length == 1 && !IsEditing && IsDataSheetActive)
        {
            char c = e.Key[0];
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c))
            {
                var inputPosition = Sheet?.GetInputForSelection();
                if (inputPosition == null)
                    return false;
                BeginEdit(inputPosition.Row, inputPosition.Col, softEdit: true, EditEntryMode.Key, e.Key);
                StateHasChanged();
            }

            return true;
        }

        return false;
    }

    private void NextTick(Action action)
    {
        QueuedActions.Enqueue(action);
    }

    Task IHandleEvent.HandleEventAsync(
        EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    public void Dispose()
    {
        _WindowEventService.Dispose();
    }
}