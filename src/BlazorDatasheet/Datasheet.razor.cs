using BlazorDatasheet.Edit;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Model;
using BlazorDatasheet.Render;
using BlazorDatasheet.Services;
using BlazorDatasheet.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet;

public partial class Datasheet : IHandleEvent
{
    [Parameter] public Sheet? Sheet { get; set; }
    private DynamicComponent? _activeEditorReference;
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<CellChangedEventArgs> OnCellChanged { get; set; }
    private BaseEditorComponent? ActiveEditorReference => (BaseEditorComponent)(_activeEditorReference.Instance);
    private bool IsDataSheetActive { get; set; }
    private CellPosition? EditPosition { get; set; }
    private EditState? EditState { get; set; }
    private bool IsEditing => EditPosition != null;
    private bool IsMouseInsideSheet { get; set; }
    private ElementReference ActiveCellInputReference;
    private Queue<Action> QueuedActions { get; set; } = new Queue<Action>();
    private Dictionary<string, Type> RenderComponentTypes { get; set; }
    private Dictionary<string, Type> EditorComponentTypes { get; set; }

    private IWindowEventService _windowEventService;

    protected override void OnInitialized()
    {
        _windowEventService = new WindowEventService(JS);
        RenderComponentTypes = new Dictionary<string, Type>();
        EditorComponentTypes = new Dictionary<string, Type>();

        RegisterRenderer<TextRenderer>("text");
        RegisterRenderer<SelectRenderer>("select");
        RegisterRenderer<NumberRenderer>("number");
        RegisterRenderer<BoolRenderer>("boolean");

        this.RegisterEditor<TextEditorComponent>("text");
        this.RegisterEditor<DateTimeEditorComponent>("datetime");
        this.RegisterEditor<BoolEditorComponent>("boolean");
        this.RegisterEditor<SelectEditorComponent>("select");

        base.OnInitialized();
    }

    public void RegisterRenderer<T>(string name) where T : BaseRenderer
    {
        if (!RenderComponentTypes.TryAdd(name, typeof(T)))
            RenderComponentTypes[name] = typeof(T);
    }

    public void RegisterEditor<T>(string name) where T : BaseEditorComponent
    {
        if (!EditorComponentTypes.TryAdd(name, typeof(T)))
            EditorComponentTypes[name] = typeof(T);
    }

    private Type getCellRendererType(string type)
    {
        if (RenderComponentTypes.ContainsKey(type))
            return RenderComponentTypes[type];

        return typeof(TextRenderer);
    }

    private Type getEditorComponentType(string type)
    {
        if (EditorComponentTypes.ContainsKey(type))
            return EditorComponentTypes[type];
        return typeof(TextEditorComponent);
    }

    private Dictionary<string, object> getCellRendererParameters(Cell cell, int row, int col)
    {
        return new Dictionary<string, object>()
        {
            { "Cell", cell },
            { "Row", row },
            { "Col", col },
            { "OnChangeCellValueRequest", HandleCellRendererRequestChangeValue },
            { "OnBeginEditRequest", HandleCellRequestBeginEdit }
        };
    }

    private Dictionary<string, object> getEditorParameters()
    {
        return new Dictionary<string, object>()
        {
            { "EditState", EditState },
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
        await _windowEventService.Init();
        _windowEventService.OnKeyDown += HandleWindowKeyDown;
        _windowEventService.OnMouseDown += HandleWindowMouseDown;
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
        if (this.IsReadOnly)
            return;

        this.Sheet.CancelSelecting();

        var cell = Sheet?.GetCell(row, col);
        if (cell == null || cell.IsReadOnly)
            return;

        EditPosition = new CellPosition() { Row = row, Col = col };
        EditState = new EditState(AcceptEdit, CancelEdit, cell);

        // Do this after the next render because the EditComponent doesn't exist until then
        NextTick(() => { ActiveEditorReference?.BeginEdit(mode, cell, entryChar); });
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

        if (ActiveEditorReference == null || EditPosition == null)
            return false;

        if (ActiveEditorReference.CanAcceptEdit())
        {
            var cell = Sheet.GetCell(EditPosition.Row, EditPosition.Col);
            cell.Value = EditState.EditString;

            Sheet.MoveSelection(dRow, dCol);
            emitCellChanged(cell, EditPosition.Row, EditPosition.Col);
            // Finish the edit
            EditPosition = null;

            StateHasChanged();

            return true;
        }

        return false;
    }

    private async void emitCellChanged(Cell cell, int row, int col)
    {
        if (!OnCellChanged.HasDelegate)
            return;
        await OnCellChanged.InvokeAsync(new CellChangedEventArgs(cell, row, col));
    }

    /// <summary>
    /// Cancels the current edit, returning whether the edit was successful
    /// </summary>
    /// <returns></returns>
    private bool CancelEdit()
    {
        if (!IsEditing)
            return false;

        if (ActiveEditorReference != null && ActiveEditorReference.CanCancelEdit())
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
            if (!IsEditing)
            {
                Sheet?.EndSelecting();
                Sheet?.MoveSelection(1, 0);
                StateHasChanged();
                return true;
            }

            if (IsEditing && AcceptEdit())
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
            else if (EditState.IsSoftEdit && AcceptEdit(direction.Item1, direction.Item2))
            {
                return true;
            }
        }

        if (e.Key == "Tab")
        {
            AcceptEdit();
            Sheet.MoveSelection(0, 1);
            StateHasChanged();
            return true;
        }

        if ((e.Key.Length == 1) && !IsEditing && IsDataSheetActive)
        {
            char c = e.Key == "Space" ? ' ' : e.Key[0];
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsSeparator(c))
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
        _windowEventService.Dispose();
    }

    /// <summary>
    /// Handles when a cell renderer requests to start editing the cell
    /// </summary>
    /// <param name="args"></param>
    private void HandleCellRequestBeginEdit(EditRequestArgs args)
    {
        BeginEdit(args.Row, args.Col, args.IsSoftEdit, args.EntryMode);
        StateHasChanged();
    }

    /// <summary>
    /// Handles when a cell renderer requests that a cell's value be changed
    /// </summary>
    /// <param name="args"></param>
    private void HandleCellRendererRequestChangeValue(ChangeCellRequestEventArgs args)
    {
        var cell = Sheet?.GetCell(args.Row, args.Col);
        cell.Value = args.NewValue;
        StateHasChanged();
        emitCellChanged(cell, args.Row, args.Col);
    }
}