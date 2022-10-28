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
using Microsoft.AspNetCore.Components.Web.Virtualization;
using ChangeEventArgs = Microsoft.AspNetCore.Components.ChangeEventArgs;

namespace BlazorDatasheet;

public partial class Datasheet : IHandleEvent
{
    /// <summary>
    /// The Sheet holding the data for the datasheet.
    /// </summary>
    [Parameter]
    public Sheet? Sheet { get; set; }

    /// <summary>
    /// Whether the row headings are sticky (only applied if the container is of fixed height)
    /// </summary>
    [Parameter]
    public bool StickyHeadings { get; set; }

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

    [Parameter] public string Theme { get; set; } = "default";

    /// <summary>
    /// Whether the user is focused on the datasheet.
    /// </summary>
    private bool IsDataSheetActive { get; set; }

    /// <summary>
    /// Whether the mouse is located inside/over the sheet.
    /// </summary>
    private bool IsMouseInsideSheet { get; set; }

    /// <summary>
    /// The selection that is in the process of being selected by the user
    /// </summary>
    private Selection? TempSelection { get; set; }

    private bool IsSelecting => TempSelection != null && !TempSelection.IsEmpty();

    private CellLayoutProvider _cellLayoutProvider;
    private EditorOverlayRenderer _editorManager;
    private IWindowEventService _windowEventService;
    private IClipboard _clipboard;

    private Virtualize<int> _virtualizer;

    // This ensures that the sheet is not re-rendered when mouse events are handled inside the sheet.
    // Performance is improved dramatically when this is used.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    protected override void OnInitialized()
    {
        _windowEventService = new WindowEventService(JS);
        _clipboard = new Clipboard(JS);
        TempSelection = new Selection(Sheet);
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // If the sheet is changed, update the references
        // to the sheet in all the managers
        if (_sheetLocal != Sheet)
        {
            if (_sheetLocal != null)
            {
                _sheetLocal.CellsChanged -= SheetOnCellsChanged;
                _sheetLocal.RowInserted -= SheetOnRowInserted;
                _sheetLocal.RowRemoved -= SheetOnRowRemoved;
            }

            _sheetLocal = Sheet;
            Sheet.CellsChanged += SheetOnCellsChanged;
            Sheet.RowInserted += SheetOnRowInserted;
            Sheet.RowRemoved += SheetOnRowRemoved;
            TempSelection.SetSheet(Sheet);
            _cellLayoutProvider = new CellLayoutProvider(Sheet, 105, 25);
        }

        base.OnParametersSet();
    }

    private async void SheetOnRowRemoved(object? sender, RowRemovedEventArgs e)
    {
        await _virtualizer.RefreshDataAsync();
    }

    private async void SheetOnRowInserted(object? sender, RowInsertedEventArgs e)
    {
        await _virtualizer.RefreshDataAsync();
    }

    private void SheetOnCellsChanged(object? sender, IEnumerable<Data.Events.ChangeEventArgs> e)
    {
        if (e.Any())
            StateHasChanged();
    }

    private Type getCellRendererType(string type)
    {
        if (Sheet?.RenderComponentTypes.ContainsKey(type) == true)
            return Sheet.RenderComponentTypes[type];

        return typeof(TextRenderer);
    }

    private Dictionary<string, object> getCellRendererParameters(IReadOnlyCell cell, int row, int col)
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await AddWindowEventsAsync();
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
        if (e.ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ActiveRegion.ExtendTo(row, col);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                Sheet?.Selection?.ClearSelections();
            }

            this.BeginSelectingCell(row, col);
        }


        if (_editorManager.IsEditing && !(_editorManager.EditRow == row && _editorManager.EditCol == col))
        {
            AcceptEdit();
        }
    }

    private void HandleColumnHeaderMouseDown(int col, MouseEventArgs e)
    {
        if (e.ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ActiveRegion.ExtendTo(0, col);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                Sheet?.Selection?.ClearSelections();
            }

            this.BeginSelectingCol(col);
        }

        AcceptEdit();
    }

    private void HandleRowHeaderMouseDown(int row, MouseEventArgs e)
    {
        if (e.ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ActiveRegion.ExtendTo(row, 0);
        else
        {
            if (!e.MetaKey && !e.CtrlKey)
            {
                Sheet?.Selection?.ClearSelections();
            }

            this.BeginSelectingRow(row);
        }

        AcceptEdit();
    }

    private async void HandleCellDoubleClick(int row, int col, MouseEventArgs e)
    {
        await BeginEdit(row, col, softEdit: false, EditEntryMode.Mouse);
    }

    private async Task BeginEdit(int row, int col, bool softEdit, EditEntryMode mode, string entryChar = "")
    {
        if (this.IsReadOnly)
            return;

        this.CancelSelecting();

        var cell = Sheet?.GetCell(row, col);
        if (cell == null || cell.IsReadOnly)
            return;

        await _editorManager.BeginEditAsync(row, col, softEdit, mode, entryChar);
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
    /// Cancels the current edit, returning whether the edit was successful
    /// </summary>
    /// <returns></returns>
    private bool CancelEdit()
    {
        return _editorManager.CancelEdit();
    }

    private void HandleCellMouseOver(int row, int col, MouseEventArgs e)
    {
        this.UpdateSelectingEndPosition(row, col);
    }

    private bool HandleWindowMouseDown(MouseEventArgs e)
    {
        bool changed = IsDataSheetActive != IsMouseInsideSheet;
        IsDataSheetActive = IsMouseInsideSheet;

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
                return true;
            }
        }

        if (KeyUtil.IsArrowKey(e.Key))
        {
            var direction = KeyUtil.GetKeyMovementDirection(e.Key);
            if (!_editorManager.IsEditing || (_editorManager.IsSoftEdit && AcceptEdit()))
            {
                this.collapseAndMoveSelection(direction.Item1, direction.Item2);
                return true;
            }
        }

        if (e.Key == "Tab")
        {
            AcceptEdit();
            this.collapseAndMoveSelection(0, 1);
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
            if (!Sheet.Selection.Regions.Any())
                return true;
            Sheet.Selection.Clear();
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
                if (Sheet == null || !Sheet.Selection.Regions.Any())
                    return false;
                var inputPosition = Sheet.Selection.ActiveCellPosition;
                if (inputPosition.IsInvalid)
                    return false;
                BeginEdit(inputPosition.Row, inputPosition.Col, softEdit: true, EditEntryMode.Key, e.Key);
            }

            return true;
        }

        return false;
    }

    private void collapseAndMoveSelection(int drow, int dcol)
    {
        if (Sheet == null)
            return;

        var activeRegion = Sheet.Selection.ActiveRegion.Collapse();
        activeRegion.Move(drow, dcol, Sheet.Region);
        Sheet.Selection.SetSingle(activeRegion);
    }

    private async Task HandleWindowPaste(PasteEventArgs arg)
    {
        if (!IsDataSheetActive)
            return;

        if (Sheet == null || !Sheet.Selection.Regions.Any())
            return;

        var posnToInput = Sheet.Selection.ActiveCellPosition;
        if (posnToInput.IsInvalid)
            return;

        var range = Sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return;

        Sheet.Selection.SetSingle(range);
    }

    public void Dispose()
    {
        _windowEventService.Dispose();
    }

    /// <summary>
    /// Handles when a cell renderer requests to start editing the cell
    /// </summary>
    /// <param name="args"></param>
    private async void HandleCellRequestBeginEdit(EditRequestArgs args)
    {
        await BeginEdit(args.Row, args.Col, args.IsSoftEdit, args.EntryMode);
    }

    /// <summary>
    /// Handles when a cell renderer requests that a cell's value be changed
    /// </summary>
    /// <param name="args"></param>
    private void HandleCellRendererRequestChangeValue(ChangeCellRequestEventArgs args)
    {
        Sheet.TrySetCellValue(args.Row, args.Col, args.NewValue);
    }

    /// <summary>
    /// Copies current selection to clipboard
    /// </summary>
    public async Task CopySelectionToClipboard()
    {
        if (this.IsSelecting || Sheet == null)
            return;

        // Can only handle single selections for now
        var region = Sheet.Selection.ActiveRegion;
        var text = Sheet.GetRegionAsDelimitedText(region);
        await _clipboard.Copy(region, Sheet);
    }


    /// <summary>
    /// Start selecting at a position (row, col). This selection is not finalised until EndSelecting() is called.
    /// </summary>
    /// <param name="row">The row where the selection should start</param>
    /// <param name="col">The col where the selection should start</param>
    private void BeginSelectingCell(int row, int col)
    {
        TempSelection.SetSingle(row, col);
    }

    private void CancelSelecting()
    {
        TempSelection.ClearSelections();
    }

    private void BeginSelectingRow(int row)
    {
        TempSelection.SetSingle(new RowRegion(row, row));
    }

    private void BeginSelectingCol(int col)
    {
        TempSelection.SetSingle(new ColumnRegion(col, col));
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

        TempSelection?.ExtendActiveRegion(row, col);
    }

    /// <summary>
    /// Ends the selecting process and adds the selection to the stack
    /// </summary>
    private void EndSelecting()
    {
        if (!IsSelecting)
            return;

        Sheet?.Selection.Add(TempSelection!.ActiveRegion!);
        TempSelection!.ClearSelections();
    }

    /// <summary>
    /// Determines whether a column contains any cells that are selected or being selected
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    private bool IsColumnActive(int col)
    {
        if (IsSelecting && TempSelection!.ActiveRegion!.SpansCol(col))
            return true;
        if (Sheet?.Selection.Regions.Any(x => x.SpansCol(col)) == true)
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
        if (IsSelecting && TempSelection!.ActiveRegion!.SpansRow(row))
            return true;
        if (Sheet?.Selection.Regions.Any(x => x.SpansRow(row)) == true)
            return true;
        return false;
    }

    /// <summary>
    /// Provides rows to the virtualised renderer
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async ValueTask<ItemsProviderResult<int>> LoadRows(
        ItemsProviderRequest request)
    {
        var numRows = request.Count;
        var startIndex = request.StartIndex;
        return new ItemsProviderResult<int>(Enumerable.Range(startIndex, numRows), Sheet.NumRows);
    }
}