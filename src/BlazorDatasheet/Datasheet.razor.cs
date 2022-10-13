using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using BlazorDatasheet.Data.Events;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.Events;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Selecting;
using BlazorDatasheet.Services;
using BlazorDatasheet.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet;

public partial class Datasheet : IHandleEvent
{
    [Parameter] public Sheet? Sheet { get; set; }

    /// <summary>
    /// Exists so that we can determine whether the sheet has changed
    /// during parameter set
    /// </summary>
    private Sheet? _sheetLocal;

    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<CellsChangedEventArgs> OnCellsChanged { get; set; }
    [Parameter] public double FixedHeightInPx { get; set; } = 350;
    [Parameter] public bool IsFixedHeight { get; set; }
    [Parameter] public bool ShowRowHeaders { get; set; } = true;
    [Parameter] public bool ShowColumnHeaders { get; set; } = true;

    private EditorManager _editorManager;
    private CommandManager _commandManager;
    private SelectionManager _selectionManager;
    private bool IsDataSheetActive { get; set; }
    private bool IsMouseInsideSheet { get; set; }
    private ElementReference ActiveCellInputReference;
    private Queue<Action> QueuedActions { get; set; } = new Queue<Action>();

    private IWindowEventService _windowEventService;
    private IClipboard _clipboard;

    protected override void OnInitialized()
    {
        _windowEventService = new WindowEventService(JS);
        _clipboard = new Clipboard(JS);
        _commandManager = new CommandManager(Sheet);
        _editorManager = new EditorManager(Sheet, _commandManager, NextTick);
        _editorManager.OnAcceptEdit += EditorManagerOnOnAcceptEdit;
        _selectionManager = new SelectionManager(Sheet);

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // If the sheet is changed, update the references
        // to the sheet in all the managers
        if (_sheetLocal != Sheet)
        {
            _sheetLocal = Sheet;
            _commandManager?.SetSheet(Sheet);
            _commandManager?.ClearHistory();
            _selectionManager?.SetSheet(Sheet);
        }

        base.OnParametersSet();
    }

    private void EditorManagerOnOnAcceptEdit(AcceptEditEventArgs e)
    {
        var cell = Sheet?.GetCell(e.Row, e.Col);
        this.emitCellsChanged(cell, e.Row, e.Col);
    }

    private Type getCellRendererType(string type)
    {
        if (Sheet?.RenderComponentTypes.ContainsKey(type) == true)
            return Sheet.RenderComponentTypes[type];

        return typeof(TextRenderer);
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
            { "EditorManager", _editorManager },
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
        _windowEventService.OnPaste += HandleWindowPaste;
    }

    private void HandleCellMouseUp(int row, int col, MouseEventArgs e)
    {
        _selectionManager?.EndSelecting();
    }

    private void HandleCellMouseDown(int row, int col, MouseEventArgs e)
    {
        if (e.ShiftKey)
            _selectionManager?.ExtendSelection(row, col);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                _selectionManager?.ClearSelections();
            }

            _selectionManager?.BeginSelectingCell(row, col);
        }


        if (_editorManager.IsEditing && !_editorManager.CurrentEditPosition.Equals(row, col))
        {
            if (AcceptEdit())
                return;
        }

        StateHasChanged();
    }

    private void HandleColumnHeaderMouseDown(int col, MouseEventArgs e)
    {
        if (e.ShiftKey)
            _selectionManager?.ExtendSelection(Sheet.NumRows - 1, col);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                _selectionManager?.ClearSelections();
            }

            _selectionManager?.BeginSelectingCol(col);
        }

        if (AcceptEdit())
            return;

        StateHasChanged();
    }

    private void HandleRowHeaderMouseDown(int row, MouseEventArgs e)
    {
        if (e.ShiftKey)
            _selectionManager?.ExtendSelection(row, Sheet.NumCols - 1);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                _selectionManager?.ClearSelections();
            }

            _selectionManager?.BeginSelectingRow(row);
        }

        if (AcceptEdit())
            return;

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

        _selectionManager.CancelSelecting();

        var cell = Sheet?.GetCell(row, col);
        if (cell == null || cell.IsReadOnly)
            return;

        _editorManager.BeginEdit(row, col, cell, softEdit, mode, entryChar);

        // Required to re-render after any edit component reference has changed
        NextTick(StateHasChanged);
    }

    private bool AcceptEdit()
    {
        return AcceptEdit(0, 0);
    }

    /// <summary>
    /// Accepts the current edit and moves the selection by dRow/dCol, returning whether the edit was successful
    /// </summary>
    /// <param name="dRow"></param>
    /// <param name="dCol"></param>
    /// <returns></returns>
    private bool AcceptEdit(int dRow, int dCol)
    {
        var result = _editorManager.AcceptEdit();
        if (!result)
            return false;

        _selectionManager.MoveSelection(dRow, dCol);
        StateHasChanged();

        return result;
    }

    /// <summary>
    /// Emit an event for a single cell change
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    private async void emitCellsChanged(Cell cell, int row, int col)
    {
        if (!OnCellsChanged.HasDelegate)
            return;
        await OnCellsChanged.InvokeAsync(new CellsChangedEventArgs(cell, row, col));
    }

    /// <summary>
    /// Emit event for all cells in ranges changed
    /// </summary>
    private async void emitCellsChanged(IEnumerable<Range> ranges)
    {
        if (!OnCellsChanged.HasDelegate)
            return;

        var infos = new List<CellChangedInfo>();
        foreach (var range in ranges)
        {
            foreach (var posn in range)
            {
                infos.Add(new CellChangedInfo(Sheet.GetCell(posn), posn.Row, posn.Col));
            }
        }

        await OnCellsChanged.InvokeAsync(new CellsChangedEventArgs(infos));
    }

    /// <summary>
    /// Emit event for all cells in the range
    /// </summary>
    private async void emitCellsChanged(Range range)
    {
        this.emitCellsChanged(new List<Range>() { range });
    }

    /// <summary>
    /// Cancels the current edit, returning whether the edit was successful
    /// </summary>
    /// <returns></returns>
    private bool CancelEdit()
    {
        var result = _editorManager.CancelEdit();
        if (result)
            StateHasChanged();

        return result;
    }

    private void HandleCellMouseOver(int row, int col, MouseEventArgs e)
    {
        _selectionManager.UpdateSelectingEndPosition(row, col);
        StateHasChanged();
    }

    private bool HandleWindowMouseDown(MouseEventArgs e)
    {
        bool changed = IsDataSheetActive != IsMouseInsideSheet;
        IsDataSheetActive = IsMouseInsideSheet;

        if (!IsDataSheetActive && AcceptEdit()) // if it is outside
        {
            changed = true;
        }

        if (changed)
            StateHasChanged();

        return false;
    }

    private bool? HandleWindowKeyDown(KeyboardEventArgs e)
    {
        if (!IsDataSheetActive)
            return false;

        var editorHandled = _editorManager.HandleKeyDown(e.Key, e.CtrlKey, e.ShiftKey, e.AltKey, e.MetaKey);
        if (editorHandled)
            return true;

        if (e.Key == "Escape")
        {
            return CancelEdit();
        }

        if (e.Key == "Enter")
        {
            if (!_editorManager.IsEditing)
            {
                _selectionManager?.EndSelecting();
                _selectionManager?.MoveSelection(1, 0);
                StateHasChanged();
                return true;
            }

            // Accept the edit
            else if (AcceptEdit(1, 0))
            {
                return true;
            }
        }

        if (KeyUtil.IsArrowKey(e.Key))
        {
            var direction = KeyUtil.GetKeyMovementDirection(e.Key);
            if (!_editorManager.IsEditing)
            {
                _selectionManager?.MoveSelection(direction.Item1, direction.Item2);
                StateHasChanged();
                return true;
            }
            // Accept the edit
            else if (_editorManager.IsSoftEdit && AcceptEdit(direction.Item1, direction.Item2))
            {
                return true;
            }
        }

        if (e.Key == "Tab")
        {
            AcceptEdit();
            _selectionManager.MoveSelection(0, 1);
            StateHasChanged();
            return true;
        }

        if (e.Key.ToLower() == "c" && (e.CtrlKey || e.MetaKey) && !_editorManager.IsEditing)
        {
            CopySelectionToClipboard();
            return true;
        }

        if (e.Key.ToLower() == "y" && (e.CtrlKey || e.MetaKey) && !_editorManager.IsEditing)
        {
            if (_commandManager.Redo())
                StateHasChanged();
            return true;
        }


        if (e.Key.ToLower() == "z" && (e.CtrlKey || e.MetaKey) && !_editorManager.IsEditing)
        {
            if (_commandManager.Undo())
                StateHasChanged();
            return true;
        }

        if ((e.Key == "Delete" || e.Key == "Backspace") && !_editorManager.IsEditing)
        {
            if (!_selectionManager.Selections.Any())
                return true;
            var rangesToClear = _selectionManager.Selections.Select(x => x.Range);
            var cmd = new ClearCellsCommand(rangesToClear);
            _commandManager.ExecuteCommand(cmd);
            StateHasChanged();
            return true;
        }

        // Single characters or numbers or symbols
        if ((e.Key.Length == 1) && !_editorManager.IsEditing && IsDataSheetActive)
        {
            // Don't input anything if we are currently selecting
            if (_selectionManager.IsSelecting)
                return false;

            // Capture commands and return early (mainly for paste)
            if (e.CtrlKey || e.MetaKey)
                return false;

            char c = e.Key == "Space" ? ' ' : e.Key[0];
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsSeparator(c))
            {
                var inputPosition = _selectionManager.GetPositionOfFirstCell();
                if (inputPosition.InvalidPosition)
                    return false;
                BeginEdit(inputPosition.Row, inputPosition.Col, softEdit: true, EditEntryMode.Key, e.Key);
                StateHasChanged();
            }

            return true;
        }

        return false;
    }

    private async Task HandleWindowPaste(PasteEventArgs arg)
    {
        if (!IsDataSheetActive)
            return;

        var posnToInput = _selectionManager.GetPositionOfFirstCell();
        if (posnToInput.InvalidPosition)
            return;

        var range = Sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return;

        this.emitCellsChanged(range);
        _selectionManager.SetSelection(range);
        this.StateHasChanged();
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
        var setValue = cell.TrySetValue(args.NewValue);
        if (!setValue)
            return;
        emitCellsChanged(cell, args.Row, args.Col);
    }

    /// <summary>
    /// Copies current selection to clipboard
    /// </summary>
    public async Task CopySelectionToClipboard()
    {
        if (_selectionManager.IsSelecting)
            return;

        // Can only handle single selections for now
        var selection = _selectionManager.GetSelections().FirstOrDefault();
        if (selection == null)
            return;
        var text = Sheet.GetRangeAsDelimitedText(selection.Range);
        await _clipboard.WriteTextAsync(text);
    }
}