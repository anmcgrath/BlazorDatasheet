using System.Text;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.Core.Util;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Services;
using BlazorDatasheet.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

namespace BlazorDatasheet;

public partial class Datasheet : SheetComponentBase
{
    /// <summary>
    /// The Sheet holding the data for the datasheet.
    /// </summary>
    [Parameter, EditorRequired]
    public Sheet Sheet { get; set; } = default!;

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
    /// When set to true (default), the sheet will be virtualised, meaning only the visible cells will be rendered.
    /// </summary>
    [Parameter]
    public bool Virtualise { get; set; } = true;

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
    /// If set to true, the user can remove rows using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserRemoveRows { get; set; } = true;

    /// <summary>
    /// If set to true, the user can remove columns using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserRemoveCols { get; set; } = true;

    /// <summary>
    /// If set to true, the user can insert rows using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserInsertRows { get; set; } = true;

    /// <summary>
    /// If set to true, the user can insert columns using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserInsertCols { get; set; } = true;

    /// <summary>
    /// If set to true, the user can sort regions using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserSort { get; set; } = true;

    /// <summary>
    /// If set to true, the user can merge regions using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserMergeRows { get; set; } = true;

    /// <summary>
    /// If set to true, the user can hide rows using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserHideRows { get; set; } = true;

    /// <summary>
    /// If set to true, the user can hide columns using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserHideCols { get; set; } = true;


    [Parameter] public bool ShowFormulaDependents { get; set; }

    /// <summary>
    /// Exists so that we can determine whether the sheet has changed
    /// during parameter set
    /// </summary>
    private Sheet _sheetLocal = default!;

    /// <summary>
    /// Set to true when the datasheet should not be edited
    /// </summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    [Parameter] public string Theme { get; set; } = "default";

    [Parameter] public Dictionary<string, RenderFragment> Icons { get; set; } = new();

    [Parameter] public RenderFragment<HeadingContext>? ColumnHeaderTemplate { get; set; }

    [Parameter] public RenderFragment? EmptyColumnsTemplate { get; set; }

    [Parameter] public RenderFragment? EmptyRowsTemplate { get; set; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Whether the user is focused on the datasheet.
    /// </summary>
    private bool IsDataSheetActive { get; set; }

    /// <summary>
    /// Whether the mouse is located inside/over the sheet.
    /// </summary>
    private bool IsMouseInsideSheet { get; set; }

    /// <summary>
    /// The total height of the VISIBLE sheet. This changes when the user scrolls or the parent scroll element is resized.
    /// </summary>
    public double RenderedInnerSheetHeight => _cellLayoutProvider
        .ComputeHeightBetween(_visualSheet.Viewport.VisibleRegion.Top, _visualSheet.Viewport.VisibleRegion.Bottom);

    /// <summary>
    /// The total width of the VISIBLE sheet. This changes when the user scrolls or the parent scroll element is resized.
    /// </summary>
    public double RenderedInnerSheetWidth => _cellLayoutProvider
        .ComputeWidthBetween(_visualSheet.Viewport.VisibleRegion.Left, _visualSheet.Viewport.VisibleRegion.Right);

    private HashSet<int> DirtyRows { get; set; } = new();

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
    private ElementReference _fillerLeft;

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
    private CellLayoutProvider _cellLayoutProvider = default!;

    /// <summary>
    /// Whether the user is actively selecting cells/rows/columns in the sheet.
    /// </summary>
    private bool IsSelecting => Sheet.Selection.IsSelecting;

    /// <summary>
    /// Manages the display of the editor, which is rendered using absolute coordinates over the top of the sheet.
    /// </summary>
    private EditorOverlayRenderer _editorManager = default!;

    /// <summary>
    /// Clipboard service that provides copy/paste functionality.
    /// </summary>
    private IClipboard _clipboard = default!;

    /// <summary>
    /// Holds visual cache information.
    /// </summary>
    private VisualSheet _visualSheet = default!;

    // This ensures that the sheet is not re-rendered when mouse events are handled inside the sheet.
    // Performance is improved dramatically when this is used.

    private SheetPointerInputService _sheetPointerInputService = null!;

    private DotNetObjectReference<Datasheet> _dotnetHelper = default!;

    private IJSObjectReference _virtualizer = null!;

    protected override void OnInitialized()
    {
        _windowEventService = new WindowEventService(JS);
        _clipboard = new Clipboard(JS);
        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        // If the sheet is changed, update the references
        // to the sheet in all the managers
        if (_sheetLocal != Sheet)
        {
            _sheetLocal = Sheet;

            _sheetLocal.SetDialogService(new SimpleDialogService(this.JS));
            _sheetLocal.Rows.Inserted += async (_, _) => await RefreshViewport();
            _sheetLocal.Columns.Inserted += async (_, _) => await RefreshViewport();
            _sheetLocal.Rows.Removed += async (_, _) => await RefreshViewport();
            _sheetLocal.Columns.Removed += async (_, _) => await RefreshViewport();
            _sheetLocal.Rows.SizeModified += async (_, _) => await RefreshViewport();
            _sheetLocal.Columns.SizeModified += async (_, _) => await RefreshViewport();

            _cellLayoutProvider = new CellLayoutProvider(_sheetLocal);
            _visualSheet = new VisualSheet(_sheetLocal);
            _visualSheet.Invalidated += (_, args) =>
            {
                DirtyRows.UnionWith(args.DirtyRows);
                this.StateHasChanged();
            };

            _cellLayoutProvider.IncludeColHeadings = ShowColHeadings;
            _cellLayoutProvider.IncludeRowHeadings = ShowRowHeadings;

            if (!Virtualise)
            {
                var vp = _cellLayoutProvider
                    .GetViewPort(0, 0,
                        _cellLayoutProvider.TotalWidth, _cellLayoutProvider.TotalHeight,
                        0, 0);

                _visualSheet.UpdateViewport(vp);
            }
        }

        base.OnParametersSet();
    }

    private Type GetCellRendererType(string type)
    {
        if (CustomCellTypeDefinitions.TryGetValue(type, out var definition))
            return definition.RendererType;

        return typeof(TextRenderer);
    }

    private Dictionary<string, object> GetCellRendererParameters(VisualCell visualCell)
    {
        return new Dictionary<string, object>()
        {
            { "Cell", visualCell },
            { "Sheet", _sheetLocal }
        };
    }

    protected override bool ShouldRender()
    {
        return SheetIsDirty || DirtyRows.Any();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetHelper = DotNetObjectReference.Create(this);

            await AddWindowEventsAsync();

            _sheetPointerInputService = new SheetPointerInputService(JS, _innerSheet);
            await _sheetPointerInputService.Init();

            _sheetPointerInputService.PointerDown += this.HandleCellMouseDown;
            _sheetPointerInputService.PointerEnter += HandleCellMouseOver;
            _sheetPointerInputService.PointerDoubleClick += HandleCellDoubleClick;

            var module =
                await JS.InvokeAsync<IJSObjectReference>("import", "./_content/BlazorDatasheet/js/virtualize.js");
            _virtualizer = await module.InvokeAsync<IJSObjectReference>("getVirtualizer");

            await _virtualizer.InvokeVoidAsync("addVirtualisationHandlers",
                _dotnetHelper,
                _wholeSheetDiv,
                nameof(HandleScroll),
                _fillerLeft,
                _fillerTop,
                _fillerRight,
                _fillerBottom);

            await module.DisposeAsync();
        }

        SheetIsDirty = false;
        DirtyRows.Clear();
    }

    /// <summary>
    /// Handles the JS interaction events
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    [JSInvokable("HandleScroll")]
    public void HandleScroll(ViewportScrollInfo e)
    {
        if (!Virtualise)
            return;

        var newViewport = _cellLayoutProvider
            .GetViewPort(
                e.ScrollLeft,
                e.ScrollTop,
                e.ContainerWidth,
                e.ContainerHeight,
                OverflowX,
                OverflowY);

        _visualSheet.UpdateViewport(newViewport);
    }

    private async Task RefreshViewport()
    {
        // Get the most up-to-date positioning of the visible sheet region in the
        // scroll container, then update the viewport to include overflows
        var vInfo =
            await _virtualizer.InvokeAsync<ViewportScrollInfo>("getViewportInfo", _wholeSheetDiv);

        var newViewport = _cellLayoutProvider
            .GetViewPort(
                vInfo.ScrollLeft,
                vInfo.ScrollTop,
                vInfo.ContainerWidth,
                vInfo.ContainerHeight,
                OverflowX,
                OverflowY);

        SheetIsDirty = true;
        _visualSheet.UpdateViewport(newViewport);
    }

    private async Task AddWindowEventsAsync()
    {
        await _windowEventService.RegisterMouseEvent("mousedown", HandleWindowMouseDown);
        await _windowEventService.RegisterKeyEvent("keydown", HandleWindowKeyDown);
        await _windowEventService.RegisterClipboardEvent("paste", HandleWindowPaste);
        await _windowEventService.RegisterMouseEvent("mouseup", HandleWindowMouseUp);
    }

    private void HandleCellMouseDown(object? sender, SheetPointerEventArgs args)
    {
        // if rmc and inside a selection, don't do anything
        if (args.MouseButton == 2 && Sheet.Selection.Contains(args.Row, args.Col))
            return;

        if (_sheetLocal.Editor.IsEditing)
        {
            if (!(Sheet.Editor.EditCell!.Row == args.Row && Sheet.Editor.EditCell!.Col == args.Col))
            {
                if (!AcceptEdit())
                    return;
            }
        }

        if (args.ShiftKey && Sheet.Selection.ActiveRegion != null)
            Sheet.Selection.ExtendTo(args.Row, args.Col);
        else
        {
            if (!args.MetaKey && !args.CtrlKey)
            {
                Sheet.Selection.ClearSelections();
            }

            if (args.Row == -1)
                this.BeginSelectingCol(args.Col);
            else if (args.Col == -1)
                this.BeginSelectingRow(args.Row);
            else
                this.BeginSelectingCell(args.Row, args.Col);

            if (args.MouseButton == 2) // RMC
                this.EndSelecting();
        }
    }

    private async void HandleCellDoubleClick(object? sender, SheetPointerEventArgs args)
    {
        if (args.Row < 0 || args.Col < 0 || args.Row >= Sheet.NumRows || args.Col >= Sheet.NumCols)
            return;

        await BeginEdit(args.Row, args.Col, EditEntryMode.Mouse);
    }

    private async Task BeginEdit(int row, int col, EditEntryMode mode, string entryChar = "")
    {
        if (this.IsReadOnly)
            return;

        this.CancelSelecting();

        var cell = Sheet.Cells.GetCell(row, col);

        if (cell.Format.IsReadOnly == true)
            return;

        // check if the cell is visible OR if the cell is merged, and part of the cell is visible
        if (!cell.IsVisible)
        {
            var mergedRegion = Sheet.Cells.GetMerge(row, col);
            if (mergedRegion == null)
                return;

            // is some part of the merge visible?
            if (Sheet.Rows.GetNextVisible(mergedRegion.Top - 1) > mergedRegion.Bottom)
                return;
            if (Sheet.Columns.GetNextVisible(mergedRegion.Left - 1) > mergedRegion.Right)
                return;
        }


        var softEdit = mode == EditEntryMode.Key || mode == EditEntryMode.None || cell.Value == null;

        Sheet.Editor.BeginEdit(row, col, softEdit, mode, entryChar);
    }

    /// <summary>
    /// Accepts the current edit returning whether the edit was successful
    /// </summary>
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

    private void HandleCellMouseOver(object? sender, SheetPointerEventArgs args)
    {
        this.UpdateSelectingEndPosition(args.Row, args.Col);
    }

    private Task<bool> HandleWindowMouseDown(MouseEventArgs e)
    {
        bool changed = IsDataSheetActive != IsMouseInsideSheet;
        IsDataSheetActive = IsMouseInsideSheet;

        if (changed)
            StateHasChanged();

        return Task.FromResult(false);
    }

    private Task<bool> HandleWindowMouseUp(MouseEventArgs arg)
    {
        EndSelecting();
        return Task.FromResult(false);
    }

    private async Task<bool> HandleWindowKeyDown(KeyboardEventArgs e)
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

        if (KeyUtil.IsEnter(e.Key))
        {
            var acceptEdit = Sheet.Editor.IsEditing && AcceptEdit();
            var movementDir = e.ShiftKey ? -1 : 1;
            Sheet.Selection.MoveActivePositionByRow(movementDir);
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
            Sheet.Selection.MoveActivePositionByCol(movementDir);
            return true;
        }

        if (e.Code == "67" /*C*/ && (e.CtrlKey || e.MetaKey) && !Sheet.Editor.IsEditing)
        {
            await CopySelectionToClipboard();
            return true;
        }

        if (e.Code == "89" /*Y*/ && (e.CtrlKey || e.MetaKey) && !Sheet.Editor.IsEditing)
        {
            return Sheet.Commands.Redo();
        }


        if (e.Code == "90" /*Z*/ && (e.CtrlKey || e.MetaKey) && !Sheet.Editor.IsEditing)
        {
            return Sheet.Commands.Undo();
        }

        if ((e.Key == "Delete" || e.Key == "Backspace") && !Sheet.Editor.IsEditing)
        {
            if (!Sheet.Selection.Regions.Any())
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
                if (!Sheet.Selection.Regions.Any())
                    return false;
                var inputPosition = Sheet.Selection.GetInputPosition();

                await BeginEdit(inputPosition.row, inputPosition.col, EditEntryMode.Key, e.Key);
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


            if (!Sheet.Selection.Regions.Any())
                return false;
            var inputPosition = Sheet.Selection.GetInputPosition();
            await BeginEdit(inputPosition.row, inputPosition.col, EditEntryMode.Key, e.Key);

            return true;
        }

        return false;
    }

    private void CollapseAndMoveSelection(int drow, int dcol)
    {
        if (Sheet.Selection.ActiveRegion == null)
            return;

        if (Sheet.Selection.IsSelecting)
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

        if (!Sheet.Selection.Regions.Any())
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
            await _windowEventService.DisposeAsync();

            _dotnetHelper.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// Copies current selection to clipboard
    /// </summary>
    public async Task CopySelectionToClipboard()
    {
        if (this.IsSelecting)
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
        Sheet.Selection.BeginSelectingCell(row, col);
    }

    private void CancelSelecting()
    {
        Sheet.Selection.CancelSelecting();
    }

    private void BeginSelectingRow(int row)
    {
        Sheet.Selection.BeginSelectingRow(row);
    }

    private void BeginSelectingCol(int col)
    {
        Sheet.Selection.BeginSelectingCol(col);
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

        if (Sheet.Selection.SelectingRegion?.End.row == row &&
            Sheet.Selection.SelectingRegion?.End.col == col)
            return;

        Sheet.Selection.UpdateSelectingEndPosition(row, col);
    }

    /// <summary>
    /// Ends the selecting process and adds the selection to the stack
    /// </summary>
    private void EndSelecting()
    {
        if (!IsSelecting)
            return;

        Sheet.Selection.EndSelecting();
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
        Sheet.Commands.ExecuteCommand(new AutoFillCommand(e.Original, e.Expanded));
    }

    private RenderFragment GetIconRenderFragment(string? cellIcon)
    {
        if (cellIcon != null && Icons.TryGetValue(cellIcon, out var rf))
            return rf;
        return _ => { };
    }
}