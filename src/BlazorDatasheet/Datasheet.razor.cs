using System.Runtime.CompilerServices;
using System.Text;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.Core.Util;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.KeyboardInput;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Services;
using BlazorDatasheet.Util;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

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
    /// The default filters that are shown when the filter interface is opened and no filter exists.
    /// </summary>
    [Parameter]
    public Type[] DefaultFilterTypes { get; set; } = [typeof(ValueFilter), typeof(PatternFilter)];

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
    /// If set to true, the user can insert columns using the context menu.f
    /// </summary>
    [Parameter]
    public bool CanUserInsertCols { get; set; } = true;

    /// <summary>
    /// If set to true, the user can sort regions using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserSort { get; set; } = true;

    /// <summary>
    /// If set to true, the user can filter columns using the context menu.
    /// </summary>
    [Parameter]
    public bool CanUserFilter { get; set; } = true;

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
    /// Fired when the Datasheet becomes active or inactive (able to receive keyboard inputs).
    /// </summary>
    [Parameter]
    public EventCallback<SheetActiveEventArgs> OnSheetActiveChanged { get; set; }

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

    /// <summary>
    /// The datasheet keyboard shortcut manager
    /// </summary>
    public ShortcutManager ShortcutManager { get; } = new();

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

    private bool _refreshViewportRequested = false;
    private bool _renderRequested = false;

    protected override void OnInitialized()
    {
        _clipboard = new Clipboard(JS);
        RegisterDefaultShortcuts();
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
            _sheetLocal.ScreenUpdatingChanged += ScreenUpdatingChanged;
            _sheetLocal.Editor.EditBegin += async (_, _) => await _windowEventService.CancelPreventDefault("keydown");

            _sheetLocal.Editor.EditFinished +=
                async (_, _) => await _windowEventService.PreventDefault("keydown");

            _cellLayoutProvider = new CellLayoutProvider(_sheetLocal);


            _visualSheet = new VisualSheet(_sheetLocal);
            _visualSheet.Invalidated += (_, args) =>
            {
                DirtyRows.UnionWith(args.DirtyRows);
                this.StateHasChanged();
            };

            if (!Virtualise)
            {
                var vp = _cellLayoutProvider
                    .GetViewPort(0, 0,
                        _cellLayoutProvider.TotalWidth, _cellLayoutProvider.TotalHeight,
                        0, 0);

                _visualSheet.UpdateViewport(vp);
            }
        }

        _cellLayoutProvider.IncludeColHeadings = ShowColHeadings;
        _cellLayoutProvider.IncludeRowHeadings = ShowRowHeadings;

        base.OnParametersSet();
    }

    private async Task ScrollToContainRegion(IRegion region)
    {
        var left = _cellLayoutProvider.ComputeLeftPosition(region);
        var top = _cellLayoutProvider.ComputeTopPosition(region);
        var right = _cellLayoutProvider.ComputeRightPosition(region);
        var bottom = _cellLayoutProvider.ComputeBottomPosition(region);

        var scrollInfo = await _virtualizer.InvokeAsync<ViewportScrollInfo>("getViewportInfo", _wholeSheetDiv);
        if (ShowRowHeadings && StickyHeadings)
        {
            scrollInfo.VisibleLeft += _cellLayoutProvider.RowHeadingWidth;
            scrollInfo.ContainerWidth -= _cellLayoutProvider.RowHeadingWidth;
        }

        if (ShowColHeadings && StickyHeadings)
        {
            scrollInfo.VisibleTop += _cellLayoutProvider.ColHeadingHeight;
            scrollInfo.ContainerHeight -= _cellLayoutProvider.ColHeadingHeight;
        }

        double scrollToY = scrollInfo.ParentScrollTop;
        double scrollToX = scrollInfo.ParentScrollLeft;

        bool doScroll = false;

        if (top < scrollInfo.VisibleTop || bottom > scrollInfo.VisibleTop + scrollInfo.ContainerHeight)
        {
            var bottomDist = bottom - (scrollInfo.VisibleTop + scrollInfo.ContainerHeight);
            var topDist = top - scrollInfo.VisibleTop;

            var scrollYDist = Math.Abs(bottomDist) < Math.Abs(topDist)
                ? bottomDist
                : topDist;

            scrollToY = Math.Round(scrollInfo.ParentScrollTop + scrollYDist, 1);
            doScroll = true;
        }

        if (left < scrollInfo.VisibleLeft || right > scrollInfo.VisibleLeft + scrollInfo.ContainerWidth)
        {
            var rightDist = right - (scrollInfo.VisibleLeft + scrollInfo.ContainerWidth);
            var leftDist = left - scrollInfo.VisibleLeft;

            var scrollXDist = Math.Abs(rightDist) < Math.Abs(leftDist)
                ? rightDist
                : leftDist;

            scrollToX = Math.Round(scrollInfo.ParentScrollLeft + scrollXDist, 1);
            doScroll = true;
        }

        if (doScroll)
            await _virtualizer.InvokeVoidAsync("scrollTo", _wholeSheetDiv, scrollToX, scrollToY, "instant");
    }

    private async void ScreenUpdatingChanged(object? sender, SheetScreenUpdatingEventArgs e)
    {
        if (e.IsScreenUpdating && _refreshViewportRequested)
            await RefreshViewport();

        if (e.IsScreenUpdating && _renderRequested)
            this.StateHasChanged();
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
        _renderRequested = true;
        if (!Sheet.ScreenUpdating)
            return false;

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

        _renderRequested = false;
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
                e.SheetLeft,
                e.SheetTop,
                e.ContainerWidth,
                e.ContainerHeight,
                OverflowX,
                OverflowY);

        _visualSheet.UpdateViewport(newViewport);
    }

    private async Task RefreshViewport()
    {
        _refreshViewportRequested = true;
        if (!Sheet.ScreenUpdating)
            return;

        // Get the most up-to-date positioning of the visible sheet region in the
        // scroll container, then update the viewport to include overflows
        var vInfo =
            await _virtualizer.InvokeAsync<ViewportScrollInfo>("getViewportInfo", _wholeSheetDiv);

        var newViewport = _cellLayoutProvider
            .GetViewPort(
                vInfo.SheetLeft,
                vInfo.SheetTop,
                vInfo.ContainerWidth,
                vInfo.ContainerHeight,
                OverflowX,
                OverflowY);

        SheetIsDirty = true;
        _visualSheet.UpdateViewport(newViewport);
        _refreshViewportRequested = false;
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
                if (!Sheet.Editor.AcceptEdit())
                    return;
            }
        }

        if (args.ShiftKey && Sheet.Selection.ActiveRegion != null)
        {
            Sheet.Selection.ExtendTo(args.Row, args.Col);
        }
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
        Sheet.Editor.BeginEdit(row, col, false, mode, entryChar);
    }

    private void HandleCellMouseOver(object? sender, SheetPointerEventArgs args)
    {
        this.UpdateSelectingEndPosition(args.Row, args.Col);
    }

    private async Task<bool> HandleWindowMouseDown(MouseEventArgs e)
    {
        bool changed = IsDataSheetActive != IsMouseInsideSheet;
        await SetActiveAsync(IsMouseInsideSheet);

        if (changed)
            StateHasChanged();

        return false;
    }

    private Task<bool> HandleWindowMouseUp(MouseEventArgs arg)
    {
        EndSelecting();
        return Task.FromResult(false);
    }

    internal async Task<bool> HandleShortcuts(string key, KeyboardModifiers modifiers)
    {
        return await ShortcutManager.ExecuteAsync(key, modifiers,
            new ShortcutExecutionContext(this, this.Sheet));
    }

    private void RegisterDefaultShortcuts()
    {
        ShortcutManager.Register(["Escape"], KeyboardModifiers.Any,
            context => Sheet.Editor.CancelEdit());

        ShortcutManager
            .Register(["Enter"], KeyboardModifiers.None, _ => AcceptEditAndMoveActiveSelection(Axis.Row, 1));
        ShortcutManager
            .Register(["Enter"], KeyboardModifiers.Shift, _ => AcceptEditAndMoveActiveSelection(Axis.Row, -1));

        ShortcutManager
            .Register(["Tab"], KeyboardModifiers.None, _ => AcceptEditAndMoveActiveSelection(Axis.Col, 1));
        ShortcutManager
            .Register(["Tab"], KeyboardModifiers.Shift, _ => AcceptEditAndMoveActiveSelection(Axis.Col, -1));

        ShortcutManager
            .Register(["KeyC"], [KeyboardModifiers.Ctrl, KeyboardModifiers.Meta],
                async (_) => await CopySelectionToClipboard(),
                _ => !Sheet.Editor.IsEditing);

        ShortcutManager
            .Register(["ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft"], KeyboardModifiers.None,
                c =>
                    HandleArrowKeysDown(false, KeyUtil.GetMovementFromArrowKey(c.Key)));

        ShortcutManager
            .Register(["ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft"], KeyboardModifiers.Shift,
                c =>
                    HandleArrowKeysDown(true, KeyUtil.GetMovementFromArrowKey(c.Key)));

        ShortcutManager.Register(["KeyY"], [KeyboardModifiers.Ctrl, KeyboardModifiers.Meta], _ => Sheet.Commands.Redo(),
            _ => !Sheet.Editor.IsEditing);
        ShortcutManager.Register(["KeyZ"], [KeyboardModifiers.Ctrl, KeyboardModifiers.Meta], _ => Sheet.Commands.Undo(),
            _ => !Sheet.Editor.IsEditing);

        ShortcutManager.Register(["Delete", "Backspace"], KeyboardModifiers.Any,
            _ => Sheet.Commands.ExecuteCommand(new ClearCellsCommand(Sheet.Selection.Regions)),
            _ => Sheet.Selection.Regions.Any());
    }

    private async Task<bool> HandleArrowKeysDown(bool shift, Offset offset)
    {
        var accepted = true;
        if (Sheet.Editor.IsEditing)
            accepted = _editorManager.IsSoftEdit && Sheet.Editor.AcceptEdit();

        if (!accepted) return false;

        if (shift)
        {
            var oldActiveRegion = Sheet.Selection.ActiveRegion?.Clone();
            GrowActiveSelection(offset);
            if (oldActiveRegion == null) return false;
            var r = Sheet.Selection.ActiveRegion!.Break(oldActiveRegion).FirstOrDefault();

            if (r == null)
            {
                // if r is null we are instead shrinking the region, so instead break the old region with the new
                // but we contract the new region to ensure that it is now visible
                var rNew = Sheet.Selection.ActiveRegion.Clone();
                Edge edge = Edge.None;
                if (offset.Rows == 1)
                    edge = Edge.Top;
                else if (offset.Rows == -1)
                    edge = Edge.Bottom;
                else if (offset.Columns == 1)
                    edge = Edge.Left;
                else if (offset.Columns == -1)
                    edge = Edge.Right;

                rNew.Contract(edge, 1);
                r = oldActiveRegion.Break(rNew).FirstOrDefault();
            }

            if (r != null && IsDataSheetActive)
                await ScrollToContainRegion(r);
        }
        else
        {
            CollapseAndMoveSelection(offset);
            if (IsDataSheetActive)
                await ScrollToActiveCellPosition();
        }

        return true;
    }

    private async Task<bool> AcceptEditAndMoveActiveSelection(Axis axis, int amount)
    {
        var acceptEdit = !Sheet.Editor.IsEditing || Sheet.Editor.AcceptEdit();
        Sheet.Selection.MoveActivePosition(axis, amount);
        if (IsDataSheetActive)
            await ScrollToActiveCellPosition();
        return acceptEdit;
    }

    private async Task ScrollToActiveCellPosition()
    {
        var cellRect =
            Sheet.Cells.GetMerge(Sheet.Selection.ActiveCellPosition.row, Sheet.Selection.ActiveCellPosition.col) ??
            new Region(Sheet.Selection.ActiveCellPosition.row, Sheet.Selection.ActiveCellPosition.col);
        await ScrollToContainRegion(cellRect);
    }

    private async Task<bool> HandleWindowKeyDown(KeyboardEventArgs e)
    {
        if (!IsDataSheetActive)
            return false;

        if (_menuService.IsMenuOpen())
            return false;

        var editorHandled = _editorManager.HandleKeyDown(e.Key, e.CtrlKey, e.ShiftKey, e.AltKey, e.MetaKey);
        if (editorHandled)
            return true;

        var modifiers = e.GetModifiers();
        if (await HandleShortcuts(e.Key, modifiers) || await HandleShortcuts(e.Code, modifiers))
            return true;

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

        return false;
    }

    /// <summary>
    /// Increases the size of the active selection, around the active cell position
    /// </summary>
    private void GrowActiveSelection(Offset offset)
    {
        if (Sheet.Selection.ActiveRegion == null)
            return;

        var selPosition = Sheet.Selection.ActiveCellPosition;
        if (offset.Columns != 0)
        {
            if (offset.Columns == -1)
            {
                if (selPosition.col < Sheet.Selection.ActiveRegion.GetEdge(Edge.Right).Right)
                    Sheet.Selection.ContractEdge(Edge.Right, 1);
                else
                    Sheet.Selection.ExpandEdge(Edge.Left, 1);
            }
            else if (offset.Columns == 1)
                if (selPosition.col > Sheet.Selection.ActiveRegion.GetEdge(Edge.Left).Left)
                    Sheet.Selection.ContractEdge(Edge.Left, 1);
                else
                    Sheet.Selection.ExpandEdge(Edge.Right, 1);
        }

        if (offset.Rows != 0)
        {
            if (offset.Rows == -1)
            {
                if (selPosition.row < Sheet.Selection.ActiveRegion.GetEdge(Edge.Bottom).Bottom)
                    Sheet.Selection.ContractEdge(Edge.Bottom, 1);
                else
                    Sheet.Selection.ExpandEdge(Edge.Top, 1);
            }
            else if (offset.Rows == 1)
            {
                if (selPosition.row > Sheet.Selection.ActiveRegion.GetEdge(Edge.Top).Top)
                    Sheet.Selection.ContractEdge(Edge.Top, 1);
                else
                    Sheet.Selection.ExpandEdge(Edge.Bottom, 1);
            }
        }
    }

    private void CollapseAndMoveSelection(Offset offset)
    {
        if (Sheet.Selection.ActiveRegion == null)
            return;

        if (Sheet.Selection.IsSelecting)
            return;

        var posn = Sheet.Selection.ActiveCellPosition;

        Sheet.Selection.Set(posn.row, posn.col);
        Sheet.Selection.MoveActivePositionByRow(offset.Rows);
        Sheet.Selection.MoveActivePositionByCol(offset.Columns);

        return;
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
    public async Task<bool> CopySelectionToClipboard()
    {
        if (this.IsSelecting)
            return false;

        // Can only handle single selections for now
        var region = Sheet.Selection.ActiveRegion;
        if (region == null)
            return false;

        await _clipboard.Copy(region, Sheet);
        return true;
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
    public async void ForceReRender()
    {
        await RefreshViewport();
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

    /// <summary>
    /// Set the datasheet as active, which controls whether the sheet is ready to receive keyboard input events.
    /// </summary>
    /// <param name="value"></param>
    public async Task SetActiveAsync(bool value = true)
    {
        if (value == IsDataSheetActive)
            return;

        if (value)
            await _windowEventService.PreventDefault("keydown");
        else
            await _windowEventService.CancelPreventDefault("keydown");

        IsDataSheetActive = value;
        await OnSheetActiveChanged.InvokeAsync(new SheetActiveEventArgs(this, value));
    }
}