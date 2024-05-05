using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.Core.Util;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Events;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Services;
using BlazorDatasheet.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using ChangeEventArgs = Microsoft.AspNetCore.Components.ChangeEventArgs;
using Microsoft.JSInterop;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

namespace BlazorDatasheet;

public partial class Datasheet : IHandleEvent
{
    private double lastRenderTime;

    /// <summary>
    /// The Sheet holding the data for the datasheet.
    /// </summary>
    [Parameter]
    public Sheet? Sheet { get; set; }

    /// <summary>
    /// Whether to show the row headings.
    /// </summary>
    [Parameter]
    public bool ShowRowHeadings { get; set; } = true;

    /// <summary>
    /// Whether to show the column headings.
    /// </summary>
    [Parameter]
    public bool ShowColHeadings { get; set; } = true;

    /// <summary>
    /// Whether the row headings are sticky (only applied if the container is of fixed height)
    /// </summary>
    [Parameter]
    public bool StickyHeadings { get; set; }

    /// <summary>
    /// Renders a number of cells past the visual region, to improve scroll performance.
    /// </summary>
    [Parameter]
    public int OverflowX { get; set; } = 2;

    /// <summary>
    /// Renders a number of cells past the visual region, to improve scroll performance.
    /// </summary>
    [Parameter]
    public int OverflowY { get; set; } = 6;

    /// <summary>
    /// Register custom editor components (derived from <see cref="BaseEditor"/>) that will be selected
    /// based on the cell type.
    /// </summary>
    [Parameter]
    public Dictionary<string, CellTypeDefinition> CustomCellTypeDefinitions { get; set; } = new();

    /// <summary>
    /// Default editors for cell types.
    /// </summary>
    private Dictionary<string, CellTypeDefinition> _defaultCellTypeDefinitions { get; } = new();

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

    [Parameter] public string Theme { get; set; } = "default";

    [Parameter] public Dictionary<string, RenderFragment> Icons { get; set; } = new();

    /// <summary>
    /// Whether the user is focused on the datasheet.
    /// </summary>
    private bool IsDataSheetActive { get; set; }

    /// <summary>
    /// Whether the mouse is located inside/over the sheet.
    /// </summary>
    private bool IsMouseInsideSheet { get; set; }

    /// <summary>
    /// The Viewport
    /// </summary>
    public readonly Viewport Viewport = new();

    /// <summary>
    /// The total height of the VISIBLE sheet. This changes when the user scrolls or the parent scroll element is resized.
    /// </summary>
    public double RenderedInnerSheetHeight => _cellLayoutProvider
        .ComputeHeightBetween(Viewport.VisibleRegion.Top, Viewport.VisibleRegion.Bottom);

    /// <summary>
    /// The total width of the VISIBLE sheet. This changes when the user scrolls or the parent scroll element is resized.
    /// </summary>
    public double RenderedInnerSheetWidth => _cellLayoutProvider
        .ComputeWidthBetween(Viewport.VisibleRegion.Left, Viewport.VisibleRegion.Right);

    /// <summary>
    /// Store any cells that are dirty here
    /// </summary>
    private HashSet<CellPosition> DirtyCells { get; set; } = new();

    /// <summary>
    /// Div that is the width/height of all the rows/columns in the sheet (does not include row/col headings).
    /// </summary>
    private ElementReference _wholeSheetDiv;

    /// <summary>
    /// Top filler element that is used for virtualisation.
    /// </summary>
    private ElementReference _fillerTop;

    /// <summary>
    /// Left filler element that is used for virtualisation.
    /// </summary>
    private ElementReference _fillerLeft1;

    /// <summary>
    /// Bottom filler element that is used for virtualisation.
    /// </summary>
    private ElementReference _fillerBottom;

    /// <summary>
    /// Right filler element that is used for virtualisation.
    /// </summary>
    private ElementReference _fillerRight;

    /// <summary>
    /// Sheet cell renderer
    /// </summary>
    private ElementReference _innerSheet;

    /// <summary>
    /// Whether the entire sheet is dirty
    /// </summary>
    public bool SheetIsDirty { get; set; } = true;

    /// <summary>
    /// Contains positioning calculations
    /// </summary>
    private CellLayoutProvider? _cellLayoutProvider;

    /// <summary>
    /// Whether the user is actively selecting cells/rows/columns in the sheet.
    /// </summary>
    internal bool IsSelecting => Sheet != null && Sheet.Selection.IsSelecting;

    /// <summary>
    /// Manages the display of the editor, which is rendered using absolute coordinates over the top of the sheet.
    /// </summary>
    private EditorOverlayRenderer _editorManager;

    /// <summary>
    /// Clipboard service that provides copy/paste functionality.
    /// </summary>
    private IClipboard _clipboard;

    /// <summary>
    /// Holds visual cache information.
    /// </summary>
    private VisualSheet _visualSheet;

    // This ensures that the sheet is not re-rendered when mouse events are handled inside the sheet.
    // Performance is improved dramatically when this is used.

    private SheetPointerInputService _sheetPointerInputService;

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

    private IJSObjectReference _virtualizer = null!;

    protected override void OnInitialized()
    {
        _windowEventService = new WindowEventService(JS);
        _clipboard = new Clipboard(JS);
        this.RegisterDefaultCellRendererAndEditors();
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // If the sheet is changed, update the references
        // to the sheet in all the managers
        if (_sheetLocal != Sheet)
        {
            _sheetLocal = Sheet;
            _sheetLocal?.SetDialogService(new SimpleDialogService(this.JS));
            _cellLayoutProvider = new CellLayoutProvider(_sheetLocal);
            _visualSheet = new VisualSheet(_sheetLocal);
            _visualSheet.Invalidated += (sender, args) =>
            {
                DirtyCells.UnionWith(args.DirtyCells);
                this.StateHasChanged();
            };

            _cellLayoutProvider.IncludeColHeadings = ShowColHeadings;
            _cellLayoutProvider.IncludeRowHeadings = ShowRowHeadings;
        }

        if (_cellLayoutProvider != null)
        {
            _cellLayoutProvider.IncludeColHeadings = ShowColHeadings;
            _cellLayoutProvider.IncludeRowHeadings = ShowRowHeadings;
        }

        base.OnParametersSet();
    }

    private void RegisterDefaultCellRendererAndEditors()
    {
        _defaultCellTypeDefinitions.Add("text", CellTypeDefinition.Create<TextEditorComponent, TextRenderer>());
        _defaultCellTypeDefinitions.Add("datetime", CellTypeDefinition.Create<DateTimeEditorComponent, TextRenderer>());
        _defaultCellTypeDefinitions.Add("boolean", CellTypeDefinition.Create<BoolEditorComponent, BoolRenderer>());
        _defaultCellTypeDefinitions.Add("select", CellTypeDefinition.Create<SelectEditorComponent, SelectRenderer>());
        _defaultCellTypeDefinitions.Add("textarea", CellTypeDefinition.Create<TextareaEditorComponent, TextRenderer>());
    }

    private Type GetCellRendererType(string type)
    {
        // First look at any custom renderers
        if (CustomCellTypeDefinitions.ContainsKey(type))
            return CustomCellTypeDefinitions[type].RendererType;

        if (_defaultCellTypeDefinitions.ContainsKey(type))
            return _defaultCellTypeDefinitions[type].RendererType;

        return typeof(TextRenderer);
    }

    private Dictionary<string, object> getCellRendererParameters(Sheet sheet, VisualCell visualCell)
    {
        return new Dictionary<string, object>()
        {
            { "Cell", visualCell },
            { "OnChangeCellValueRequest", HandleCellRendererRequestChangeValue },
            { "OnBeginEditRequest", HandleCellRequestBeginEdit }
        };
    }

    protected override bool ShouldRender()
    {
        return SheetIsDirty || DirtyCells.Any();
    }

    private DotNetObjectReference<Datasheet> _dotnetHelper;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetHelper = DotNetObjectReference.Create(this);

            await AddWindowEventsAsync();

            _sheetPointerInputService = new SheetPointerInputService(JS, _innerSheet);
            await _sheetPointerInputService.Init();

            _sheetPointerInputService.PointerDown += (sender, args) =>
                this.HandleCellMouseDown(args.Row, args.Col, args.MetaKey, args.CtrlKey, args.ShiftKey);
            _sheetPointerInputService.PointerUp += (sender, args) =>
                this.HandleCellMouseUp(args.Row, args.Col, args.MetaKey, args.CtrlKey, args.ShiftKey);
            _sheetPointerInputService.PointerEnter += (sender, args) =>
                this.HandleCellMouseOver(args.Row, args.Col);
            _sheetPointerInputService.PointerDoubleClick += (sender, args) =>
                this.HandleCellDoubleClick(args.Row, args.Col, args.MetaKey, args.CtrlKey, args.ShiftKey);

            var module =
                await JS.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/virtualize.js");
            _virtualizer = await module.InvokeAsync<IJSObjectReference>("getVirtualizer");

            await _virtualizer.InvokeVoidAsync("addVirtualisationHandlers",
                _dotnetHelper,
                _wholeSheetDiv,
                nameof(HandleScroll),
                _fillerLeft1,
                _fillerTop,
                _fillerRight,
                _fillerBottom);
            await module.DisposeAsync();
        }

        SheetIsDirty = false;
        DirtyCells.Clear();
    }

    /// <summary>
    /// Handles the JS interaction events
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    [JSInvokable("HandleScroll")]
    public void HandleScroll(ScrollEvent e)
    {
        var newViewport = _cellLayoutProvider
            .GetViewPort(
                e.ScrollLeft,
                e.ScrollTop,
                e.ContainerWidth,
                e.ContainerHeight,
                OverflowX,
                OverflowY);
        
        Viewport.Update(newViewport);
        _visualSheet.UpdateViewport(_sheetLocal!, newViewport);
    }

    private string GetAbsoluteCellPositionStyles(int row, int col, int rowSpan, int colSpan)
    {
        var sb = new StringBuilder();
        var top = _cellLayoutProvider.ComputeTopPosition(row);
        var left = _cellLayoutProvider.ComputeLeftPosition(col);
        sb.Append("position:absolute;");
        sb.Append($"top:{top}px;");
        sb.Append($"width:{_cellLayoutProvider.ComputeWidth(col, colSpan)}px;");
        sb.Append($"height:{_cellLayoutProvider.ComputeHeight(row, rowSpan)}px;");
        sb.Append($"left:{left}px;");
        return sb.ToString();
    }

    private async Task AddWindowEventsAsync()
    {
        await _windowEventService.RegisterMouseEvent("mousedown", HandleWindowMouseDown);
        await _windowEventService.RegisterKeyEvent("keydown", HandleWindowKeyDown);
        await _windowEventService.RegisterClipboardEvent("paste", HandleWindowPaste);
    }

    private void HandleCellMouseUp(int row, int col, bool MetaKey, bool CtrlKey, bool ShiftKey)
    {
        this.EndSelecting();
    }

    private void HandleCellMouseDown(int row, int col, bool MetaKey, bool CtrlKey, bool ShiftKey)
    {
        if (_sheetLocal.Editor.IsEditing)
        {
            if (!(Sheet.Editor.EditCell.Row == row && Sheet.Editor.EditCell.Col == col))
            {
                if (!AcceptEdit())
                    return;
            }
        }

        if (ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ExtendTo(row, col);
        else
        {
            if (!MetaKey && !CtrlKey)
            {
                Sheet?.Selection?.ClearSelections();
            }

            var mergeRangeAtPosition = _sheetLocal.Cells.GetMerge(row, col);
            if (row == -1)
                this.BeginSelectingCol(col);
            else if (col == -1)
                this.BeginSelectingRow(row);
            else
                this.BeginSelectingCell(row, col);
        }
    }

    private void HandleColumnHeaderMouseDown(ColumnMouseEventArgs args)
    {
        this.HandleCellMouseDown(-1, args.Column, args.Args.MetaKey, args.Args.CtrlKey, args.Args.ShiftKey);
    }

    private void HandleColumnHeaderMouseUp(ColumnMouseEventArgs args)
    {
        this.HandleCellMouseUp(-1, args.Column, args.Args.MetaKey, args.Args.CtrlKey, args.Args.ShiftKey);
    }

    private void HandleColumnHeaderMouseOver(ColumnMouseEventArgs args)
    {
        this.HandleCellMouseOver(-1, args.Column);
    }

    private void HandleRowHeaderMouseDown(RowMouseEventArgs e)
    {
        this.HandleCellMouseDown(e.RowIndex, -1, e.Args.MetaKey, e.Args.CtrlKey, e.Args.ShiftKey);
    }

    private void HandleRowHeaderMouseUp(RowMouseEventArgs args)
    {
        this.HandleCellMouseUp(args.RowIndex, -1, args.Args.MetaKey, args.Args.CtrlKey, args.Args.ShiftKey);
    }

    private void HandleRowHeaderMouseOver(RowMouseEventArgs args)
    {
        this.HandleCellMouseOver(args.RowIndex, -1);
    }

    private async void HandleCellDoubleClick(int row, int col, bool metaKey, bool ctrlKey, bool shiftKey)
    {
        await BeginEdit(row, col, EditEntryMode.Mouse);
    }

    private async Task BeginEdit(int row, int col, EditEntryMode mode, string entryChar = "")
    {
        if (this.IsReadOnly)
            return;

        this.CancelSelecting();

        var cell = Sheet?.Cells?.GetCell(row, col);

        if (cell == null || cell.Format?.IsReadOnly == true)
            return;

        var softEdit = mode == EditEntryMode.Key || mode == EditEntryMode.None || cell.Value == null;

        Sheet?.Editor.BeginEdit(row, col, softEdit, mode, entryChar);
    }

    /// <summary>
    /// Accepts the current edit returning whether the edit was successful
    /// </summary>
    /// <param name="dRow"></param>
    /// <returns></returns>
    private bool AcceptEdit()
    {
        return Sheet.Editor.AcceptEdit();
    }

    /// <summary>
    /// Cancels the current edit, returning whether the edit was successful
    /// </summary>
    /// <returns></returns>
    private bool CancelEdit()
    {
        return Sheet.Editor.CancelEdit();
    }

    private void HandleCellMouseOver(int row, int col)
    {
        this.UpdateSelectingEndPosition(row, col);
    }

    private Task<bool> HandleWindowMouseDown(MouseEventArgs e)
    {
        bool changed = IsDataSheetActive != IsMouseInsideSheet;
        IsDataSheetActive = IsMouseInsideSheet;

        if (changed)
            StateHasChanged();

        return Task.FromResult(false);
    }

    private async Task<bool> HandleWindowKeyDown(KeyboardEventArgs e)
    {
        if (Sheet == null)
            return false;

        if (!IsDataSheetActive)
            return false;

        var editorHandled = _editorManager.HandleKeyDown(e.Key, e.CtrlKey, e.ShiftKey, e.AltKey, e.MetaKey);
        if (editorHandled)
            return true;

        if (e.Key == "Escape")
        {
            return CancelEdit();
        }

        if (KeyUtil.IsEnter(e.Key))
        {
            var acceptEdit = Sheet.Editor.IsEditing && AcceptEdit();
            var movementDir = e.ShiftKey ? -1 : 1;
            Sheet?.Selection?.MoveActivePositionByRow(movementDir);
            return acceptEdit;
        }

        if (KeyUtil.IsArrowKey(e.Key))
        {
            var direction = KeyUtil.GetKeyMovementDirection(e.Key);
            if (!Sheet.Editor.IsEditing || (_editorManager.IsSoftEdit && AcceptEdit()))
            {
                this.CollapseAndMoveSelection(direction.Item1, direction.Item2);
                return true;
            }
        }

        if (e.Key == "Tab" && (!Sheet.Editor.IsEditing || AcceptEdit()))
        {
            var movementDir = e.ShiftKey ? -1 : 1;
            Sheet?.Selection?.MoveActivePositionByCol(movementDir);
            return true;
        }

        if (e.Code == "67" /*C*/ && (e.CtrlKey || e.MetaKey) && !Sheet.Editor.IsEditing)
        {
            await CopySelectionToClipboard();
            return true;
        }

        if (e.Code == "89" /*Y*/ && (e.CtrlKey || e.MetaKey) && !Sheet.Editor.IsEditing)
        {
            return Sheet!.Commands.Redo();
        }


        if (e.Code == "90" /*Z*/ && (e.CtrlKey || e.MetaKey) && !Sheet.Editor.IsEditing)
        {
            return Sheet!.Commands.Undo();
        }

        if ((e.Key == "Delete" || e.Key == "Backspace") && !Sheet.Editor.IsEditing)
        {
            if (!Sheet!.Selection.Regions.Any())
                return true;

            Sheet.Selection.Clear();
            return true;
        }

        // Single characters or numbers or symbols
        if ((e.Key.Length == 1) && !Sheet.Editor.IsEditing && IsDataSheetActive)
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
                var inputPosition = Sheet.Selection.GetInputPosition();

                BeginEdit(inputPosition.row, inputPosition.col, EditEntryMode.Key, e.Key);
            }

            return true;
        }

        // Ecxel like begin edit request by pressing F2
        if ((e.Key == "F2") && !Sheet.Editor.IsEditing && IsDataSheetActive)
        {
            // Don't input anything if we are currently selecting
            if (this.IsSelecting)
                return false;

            // Capture commands and return early (mainly for paste)
            if (e.CtrlKey || e.MetaKey)
                return false;


            if (Sheet == null || !Sheet.Selection.Regions.Any())
                return false;
            var inputPosition = Sheet.Selection.GetInputPosition();
            BeginEdit(inputPosition.row, inputPosition.col, EditEntryMode.Key, e.Key);

            return true;
        }

        return false;
    }

    private void CollapseAndMoveSelection(int drow, int dcol)
    {
        if (Sheet?.Selection?.ActiveRegion == null)
            return;

        if (Sheet?.Selection.IsSelecting == true)
            return;

        var posn = Sheet.Selection.ActiveCellPosition;
        Sheet.Selection.ClearSelections();
        Sheet.Selection.Set(posn.row, posn.col);
        Sheet.Selection.MoveActivePositionByRow(drow);
        Sheet.Selection.MoveActivePositionByCol(dcol);
    }

    private async Task<bool> HandleWindowPaste(ClipboardEventArgs arg)
    {
        if (!IsDataSheetActive)
            return false;

        if (Sheet == null || !Sheet.Selection.Regions.Any())
            return false;

        if (Sheet.Editor.IsEditing)
            return false;

        var posnToInput = Sheet.Selection.GetInputPosition();

        var range = Sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return false;

        Sheet.Selection.Set(range);
        return true;
    }

    public async void Dispose()
    {
        try
        {
            await _virtualizer.InvokeAsync<string>("disposeVirtualisationHandlers", _wholeSheetDiv);
            await _sheetPointerInputService.DisposeAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        _dotnetHelper?.Dispose();
    }

    /// <summary>
    /// Handles when a cell renderer requests to start editing the cell
    /// </summary>
    /// <param name="args"></param>
    private async void HandleCellRequestBeginEdit(CellEditRequest args)
    {
        await BeginEdit(args.Row, args.Col, args.EntryMode);
    }

    /// <summary>
    /// Handles when a cell renderer requests that a cell's value be changed
    /// </summary>
    /// <param name="args"></param>
    private void HandleCellRendererRequestChangeValue(ChangeCellValueRequest args)
    {
        Sheet.Cells.SetValue(args.Row, args.Col, args.NewValue);
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
        if (region == null)
            return;

        await _clipboard.Copy(region, Sheet);
    }


    /// <summary>
    /// Start selecting at a position (row, col). This selection is not finalised until EndSelecting() is called.
    /// </summary>
    /// <param name="row">The row where the selection should start</param>
    /// <param name="col">The col where the selection should start</param>
    private void BeginSelectingCell(int row, int col)
    {
        Sheet?.Selection.BeginSelectingCell(row, col);
    }

    private void CancelSelecting()
    {
        Sheet?.Selection.CancelSelecting();
    }

    private void BeginSelectingRow(int row)
    {
        Sheet?.Selection.BeginSelectingRow(row);
    }

    private void BeginSelectingCol(int col)
    {
        Sheet?.Selection.BeginSelectingCol(col);
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

        if (Sheet?.Selection?.SelectingRegion?.End.row == row &&
            Sheet?.Selection?.SelectingRegion?.End.col == col)
            return;

        Sheet?.Selection.UpdateSelectingEndPosition(row, col);
    }

    /// <summary>
    /// Ends the selecting process and adds the selection to the stack
    /// </summary>
    private void EndSelecting()
    {
        if (!IsSelecting)
            return;

        Sheet?.Selection.EndSelecting();
    }

    private string GetContainerClassString()
    {
        var sb = new StringBuilder();
        sb.Append(" vars sheet ");
        sb.Append(IsDataSheetActive ? " active-sheet " : " in-active-sheet ");
        return sb.ToString();
    }

    /// <summary>
    /// Re-render all cells, regardless of whether they are dirty
    /// </summary>
    public void ForceReRender()
    {
        SheetIsDirty = true;
        StateHasChanged();
    }

    private void HandleSelectionExpanded(SelectionExpandedEventArgs e)
    {
        _sheetLocal?.Commands.ExecuteCommand(new AutoFillCommand(e.Original, e.Expanded));
    }

    private RenderFragment GetIconRenderFragment(string? cellIcon)
    {
        if (cellIcon != null && Icons.TryGetValue(cellIcon, out var rf))
            return rf;
        return _ => { };
    }
}