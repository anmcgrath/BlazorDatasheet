using System.Text;
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
    /// <summary>
    /// The Sheet holding the data for the datasheet.
    /// </summary>
    [Parameter]
    public Sheet? Sheet { get; set; }

    /// <summary>
    /// Exists so that we can determine whether the sheet has changed
    /// during parameter set
    /// </summary>
    private Sheet? _sheetLocal;

    /// <summary>
    /// Set to true when the datasheet should not be edited
    /// </summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Fixed height in pixels of the datasheet, if IsFixedHeight = true. Default is 350 px.
    /// </summary>
    [Parameter]
    public double FixedHeightInPx { get; set; } = 350;

    /// <summary>
    /// Whether the datasheet should be a fixed height. If it's true, a scrollbar will be used to
    /// scroll through the rolls that are outside of the view.
    /// </summary>
    [Parameter]
    public bool IsFixedHeight { get; set; }

    /// <summary>
    /// Whether to show the row headings. Default is true.
    /// </summary>
    [Parameter]
    public bool ShowRowHeaders { get; set; } = true;

    /// <summary>
    /// Whether to show column headings. Default is true.
    /// </summary>
    [Parameter]
    public bool ShowColumnHeaders { get; set; } = true;

    /// <summary>
    /// Whether the user is focused on the datasheet.
    /// </summary>
    private bool IsDataSheetActive { get; set; }

    /// <summary>
    /// Whether the mouse is located inside/over the sheet.
    /// </summary>
    private bool IsMouseInsideSheet { get; set; }

    /// <summary>
    /// The range that is in the process of being selected via user input.
    /// </summary>
    private IRange? RangeSelecting { get; set; }

    private bool IsSelecting => RangeSelecting != null;

    /// <summary>
    /// The current list of actions that should be performed on the next render.
    /// </summary>
    private Queue<Action> QueuedActions { get; set; } = new Queue<Action>();

    private CellLayoutProvider _cellLayoutProvider;
    private EditorManager _editorManager;
    private IWindowEventService _windowEventService;
    private IClipboard _clipboard;

    [Parameter] public EventCallback<CellsChangedEventArgs> OnCellsChanged { get; set; }

    protected override void OnInitialized()
    {
        _windowEventService = new WindowEventService(JS);
        _clipboard = new Clipboard(JS);
        _editorManager = new EditorManager(Sheet, NextTick);
        _editorManager.OnAcceptEdit += EditorManagerOnOnAcceptEdit;

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // If the sheet is changed, update the references
        // to the sheet in all the managers
        if (_sheetLocal != Sheet)
        {
            _sheetLocal = Sheet;
            _editorManager.SetSheet(Sheet);
            _cellLayoutProvider = new CellLayoutProvider(Sheet, 105.6, 28, ShowColumnHeaders, ShowRowHeaders);
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
        this.EndSelecting();
    }

    private void HandleCellMouseDown(int row, int col, MouseEventArgs e)
    {
        if (e.ShiftKey && Sheet?.Selection?.ActiveRange != null)
            Sheet?.Selection?.ActiveRange.ExtendTo(row, col);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                Sheet?.Selection?.Clear();
            }

            this.BeginSelectingCell(row, col);
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
        if (e.ShiftKey && Sheet?.Selection?.ActiveRange != null)
            Sheet?.Selection?.ActiveRange.ExtendTo(0, col);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                Sheet?.Selection?.Clear();
            }

            this.BeginSelectingCol(col);
        }

        if (AcceptEdit())
            return;

        StateHasChanged();
    }

    private void HandleRowHeaderMouseDown(int row, MouseEventArgs e)
    {
        if (e.ShiftKey && Sheet?.Selection?.ActiveRange != null)
            Sheet?.Selection?.ActiveRange.ExtendTo(row, 0);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                Sheet?.Selection?.Clear();
            }

            this.BeginSelectingRow(row);
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

        this.CancelSelecting();

        var cell = Sheet?.GetCell(row, col);
        if (cell == null || cell.IsReadOnly)
            return;

        _editorManager.BeginEdit(row, col, cell, softEdit, mode, entryChar);

        // Required to re-render after any edit component reference has changed
        NextTick(StateHasChanged);
    }

    /// <summary>
    /// Accepts the current edit returning whether the edit was successful
    /// </summary>
    /// <param name="dRow"></param>
    /// <returns></returns>
    private bool AcceptEdit()
    {
        var result = _editorManager.AcceptEdit();
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
        this.UpdateSelectingEndPosition(row, col);
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
            if (!_editorManager.IsEditing || this.AcceptEdit())
            {
                var movementDir = e.ShiftKey ? -1 : 1;
                Sheet?.Selection?.MoveActivePosition(movementDir);
                StateHasChanged();
                return true;
            }
        }

        if (KeyUtil.IsArrowKey(e.Key))
        {
            var direction = KeyUtil.GetKeyMovementDirection(e.Key);
            if (!_editorManager.IsEditing || (_editorManager.IsSoftEdit && AcceptEdit()))
            {
                this.collapseAndMoveSelection(direction.Item1, direction.Item2);
                StateHasChanged();
                return true;
            }
        }

        if (e.Key == "Tab")
        {
            AcceptEdit();
            this.collapseAndMoveSelection(0, 1);
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
            if (Sheet.Commands.Redo())
                StateHasChanged();
            return true;
        }


        if (e.Key.ToLower() == "z" && (e.CtrlKey || e.MetaKey) && !_editorManager.IsEditing)
        {
            if (Sheet.Commands.Undo())
                StateHasChanged();
            return true;
        }

        if ((e.Key == "Delete" || e.Key == "Backspace") && !_editorManager.IsEditing)
        {
            if (!Sheet.Selection.Ranges.Any())
                return true;
            var rangesToClear = Sheet.Selection.Ranges;
            var cmd = new ClearCellsCommand(rangesToClear);
            Sheet.Commands.ExecuteCommand(cmd);
            StateHasChanged();
            return true;
        }

        // Single characters or numbers or symbols
        if ((e.Key.Length == 1) && !_editorManager.IsEditing && IsDataSheetActive)
        {
            // Don't input anything if we are currently selecting
            if (this.IsSelecting)
                return false;

            // Capture commands and return early (mainly for paste)
            if (e.CtrlKey || e.MetaKey)
                return false;

            char c = e.Key == "Space" ? ' ' : e.Key[0];
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsSeparator(c))
            {
                if (Sheet == null || !Sheet.Selection.Ranges.Any())
                    return false;
                var inputPosition = Sheet.Selection.ActiveCellPosition;
                if (inputPosition.InvalidPosition)
                    return false;
                BeginEdit(inputPosition.Row, inputPosition.Col, softEdit: true, EditEntryMode.Key, e.Key);
                StateHasChanged();
            }

            return true;
        }

        return false;
    }

    private void collapseAndMoveSelection(int drow, int dcol)
    {
        if (Sheet == null)
            return;

        var activeRange = Sheet.Selection.ActiveRange.Collapse();
        activeRange.Move(drow, dcol, Sheet.Range);
        Sheet.Selection.SetSingle(activeRange);
    }

    private async Task HandleWindowPaste(PasteEventArgs arg)
    {
        if (!IsDataSheetActive)
            return;
        
        if (Sheet == null || !Sheet.Selection.Ranges.Any())
            return;

        var posnToInput = Sheet.Selection.ActiveCellPosition;
        if (posnToInput.InvalidPosition)
            return;

        var range = Sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return;

        this.emitCellsChanged(range);
        Sheet.Selection.SetSingle(range);
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
        if (this.IsSelecting || Sheet == null)
            return;

        // Can only handle single selections for now
        var range = Sheet.Selection.ActiveRange;
        if (range == null)
            return;
        var text = Sheet.GetRangeAsDelimitedText(range);
        await _clipboard.WriteTextAsync(text);
    }

    /// <summary>
    /// Calculates the top/left/width/height styles of the editor container
    /// </summary>
    /// <returns></returns>
    private string GetEditorSizeStyling()
    {
        var strBuilder = new StringBuilder();

        var Position = _editorManager.CurrentEditPosition;
        var editorRange = new Range(Position.Row, Position.Col);

        var left = _cellLayoutProvider.ComputeLeftPosition(editorRange);
        var top = _cellLayoutProvider.ComputeTopPosition(editorRange);
        var w = _cellLayoutProvider.ComputeWidth(editorRange);
        var h = _cellLayoutProvider.ComputeHeight(editorRange);

        strBuilder.Append($"left:{left + 1}px;");
        strBuilder.Append($"top:{top + 1}px;");
        strBuilder.Append($"width:{w - 1}px;");
        strBuilder.Append($"height:{h - 1}px;");
        strBuilder.Append($"box-shadow: 0px 0px 5px grey");
        var style = strBuilder.ToString();
        return style;
    }


    /// <summary>
    /// Start selecting at a position (row, col). This selection is not finalised until EndSelecting() is called.
    /// </summary>
    /// <param name="row">The row where the selection should start</param>
    /// <param name="col">The col where the selection should start</param>
    private void BeginSelectingCell(int row, int col)
    {
        RangeSelecting = new Range(row, col);
    }

    private void CancelSelecting()
    {
        RangeSelecting = null;
    }

    private void BeginSelectingRow(int row)
    {
        RangeSelecting = new RowRange(row, row);
    }

    private void BeginSelectingCol(int col)
    {
        RangeSelecting = new ColumnRange(col, col);
    }

    /// <summary>
    /// Updates the current selecting selection by extending it to row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    private void UpdateSelectingEndPosition(int row, int col)
    {
        if (!IsSelecting)
            return;

        RangeSelecting?.ExtendTo(row, col);
    }

    /// <summary>
    /// Ends the selecting process and adds the selection to the stack
    /// </summary>
    private void EndSelecting()
    {
        if (!IsSelecting)
            return;

        Sheet?.Selection.Add(RangeSelecting!);
        this.RangeSelecting = null;
    }

    /// <summary>
    /// Determines whether a column contains any cells that are selected or being selected
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    private bool IsColumnActive(int col)
    {
        if (IsSelecting && RangeSelecting!.SpansCol(col))
            return true;
        if (Sheet?.Selection.Ranges.Any(x => x.SpansCol(col)) == true)
            return true;
        return false;
    }

    /// <summary>
    /// Determines whether a row contains any cells that are selected or being selected
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    private bool IsRowActive(int row)
    {
        if (IsSelecting && RangeSelecting!.SpansRow(row))
            return true;
        if (Sheet?.Selection.Ranges.Any(x => x.SpansRow(row)) == true)
            return true;
        return false;
    }
}