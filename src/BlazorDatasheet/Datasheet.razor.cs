using System.Runtime.CompilerServices;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Util;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.KeyboardInput;
using BlazorDatasheet.Menu;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.Headings;
using BlazorDatasheet.Services;
using BlazorDatasheet.Virtualise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;
using Microsoft.JSInterop;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet;

public partial class Datasheet : SheetComponentBase
{
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private IWindowEventService WindowEventService { get; set; } = null!;
    [Inject] private IMenuService MenuService { get; set; } = null!;
    private IClipboard ClipboardService { get; set; } = null!;
    

    /// <summary>
    /// The Sheet holding the data for the datasheet.
    /// </summary>
    [Parameter, EditorRequired]
    public Sheet? Sheet { get; set; }

    private Sheet _sheet = new(1, 1);

    /// <summary>
    /// When set, this restricts the datasheet to viewing this region, otherwise the datasheet views the whole sheet.
    /// </summary>
    [Parameter]
    public Region? ViewRegion { get; set; }

    private Region _viewRegion = new(0, 0);

    /// <summary>
    /// Datasheet theme that controls the css variables used to style the sheet.
    /// </summary>
    [Parameter]
    public string Theme { get; set; } = "default";

    private string _theme = "default";

    /// <summary>
    /// Renders graphics that show which cell formulas are dependent on others.
    /// </summary>
    [Parameter]
    public bool ShowFormulaDependents { get; set; }

    /// <summary>
    /// Fired when the Datasheet becomes active or inactive (able to receive keyboard inputs).
    /// </summary>
    [Parameter]
    public EventCallback<SheetActiveEventArgs> OnSheetActiveChanged { get; set; }

    /// <summary>
    /// Set to true when the datasheet should not be edited
    /// </summary>
    [Parameter]
    public bool IsReadOnly { get; set; }

    /// <summary>
    /// Whether to show the row headings.
    /// </summary>
    [Parameter]
    public bool ShowRowHeadings { get; set; } = true;

    private bool _showRowHeadings;

    /// <summary>
    /// Whether to show the column headings.
    /// </summary>
    [Parameter]
    public bool ShowColHeadings { get; set; } = true;

    private bool _showColHeadings;

    /// <summary>
    /// Specifies how many columns are frozen on the left side of the grid.
    /// </summary>
    [Parameter]
    public int FrozenLeftCount { get; set; }

    private int _frozenLeftCount;

    /// <summary>
    /// Specifies how many columns are frozen on the right side of the grid.
    /// </summary>
    [Parameter]
    public int FrozenRightCount { get; set; }

    private int _frozenRightCount;

    /// <summary>
    /// Specifies how many rows are frozen on the top side of the grid.
    /// </summary>
    [Parameter]
    public int FrozenTopCount { get; set; }

    private int _frozenTopCount;

    /// <summary>
    /// Specifies how many rows are frozen on the bottom side of the grid.
    /// </summary>
    [Parameter]
    public int FrozenBottomCount { get; set; }

    private int _frozenBottomCount;

    /// <summary>
    /// An indicator of how deep the grid is. Any sub-grid of the grid should have a higher <see cref="GridLevel"/> than its parent.
    /// This is used internally and should not be used in most circumstances.
    /// </summary>
    [Parameter]
    public int GridLevel { get; set; }

    /// <summary>
    /// Register custom editor components (derived from <see cref="BaseEditor"/>) that will be selected
    /// based on the cell type.
    /// </summary>
    [Parameter]
    public Dictionary<string, CellTypeDefinition> CustomCellTypeDefinitions { get; set; } = new();

    /// <summary>
    /// Supplies a dictionary of <seealso cref="RenderFragment"/> items that represent various icons.
    /// </summary>
    [Parameter]
    public Dictionary<string, RenderFragment> Icons { get; set; } = new();

    /// <summary>
    /// When set to true (default), the sheet will be virtualised, meaning only the visible cells will be rendered.
    /// </summary>
    [Parameter]
    public bool Virtualise { get; set; } = true;

    /// <summary>
    /// The datasheet keyboard shortcut manager
    /// </summary>
    public ShortcutManager ShortcutManager { get; } = new();

    /// <summary>
    /// Whether the user is focused on the datasheet.
    /// </summary>
    private bool IsDataSheetActive { get; set; }

    /// <summary>
    /// Whether the mouse is located inside/over the sheet.
    /// </summary>
    private bool IsMouseInsideSheet { get; set; }

    /// <summary>
    /// Whether the row and column headers are sticky
    /// </summary>
    [Parameter]
    public bool StickyHeaders { get; set; } = true;

    /// <summary>
    /// The default filters that are shown when the filter interface is opened and no filter exists.
    /// </summary>
    [Parameter]
    public Type[] DefaultFilterTypes { get; set; } = [typeof(ValueFilter), typeof(PatternFilter)];

    /// <summary>
    /// Provides menu options for the sheet
    /// </summary>
    [Parameter]
    public SheetMenuOptions MenuOptions { get; set; } = new();

    private SheetMenuOptions _menuOptions = new();

    private DotNetObjectReference<Datasheet> _dotnetHelper = default!;

    private SheetPointerInputService _sheetPointerInputService = null!;

    /// <summary>
    /// The whole sheet container, useful for checking whether mouse is inside the sheet
    /// </summary>
    private ElementReference _sheetContainer = default!;

    /// <summary>
    /// Main virtualised view
    /// </summary>
    private Virtualise2D? _mainView;

    /// <summary>
    /// The editor layer, which renders the cell editor.
    /// </summary>
    private EditorLayer _editorLayer = default!;

    private readonly List<IRegion> _dirtyRows = new();

    private bool _sheetIsDirty;

    private bool _renderRequested;

    private bool _showFormulaDependents;

    private Viewport _currentViewport = new(new(-1, -1), new(0, 0, 0, 0));

    /// <summary>
    /// Width of the sheet, including any gutters (row headings etc.)
    /// </summary>
    public double TotalSheetWidth => _sheet.Columns.GetVisualWidthBetween(0, _sheet.NumCols) + GetGutterSize(Axis.Row);

    /// <summary>
    /// Height of the sheet, including any gutters (col headings etc.)
    /// </summary>
    public double TotalSheetHeight => _sheet.Rows.GetVisualHeightBetween(0, _sheet.NumRows) + GetGutterSize(Axis.Col);

    /// <summary>
    /// The size of the main region of this datasheet, that is the region of the grid without
    /// any frozen rows or columns.
    /// </summary>
    private Region MainViewRegion => new(
        Math.Max(FrozenTopCount, _viewRegion.Top),
        Math.Min(_viewRegion.Bottom - _frozenBottomCount, _viewRegion.Bottom),
        Math.Max(FrozenLeftCount, _viewRegion.Left),
        Math.Min(_viewRegion.Right - _frozenRightCount, _viewRegion.Right));

    protected override void OnInitialized()
    {
        ClipboardService = new Clipboard(Js);
        RegisterDefaultShortcuts();
        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        bool requireRender = false;

        if (Sheet != _sheet)
        {
            RemoveEvents(_sheet);
            _sheet = Sheet ?? new(0, 0);
            AddEvents(_sheet);
            _visualCellCache.Clear();
            ForceReRender();
        }

        if (Theme != _theme)
        {
            _theme = Theme;
            requireRender = true;
        }

        if (!_viewRegion.Equals(ViewRegion))
        {
            _viewRegion = ViewRegion ?? _sheet.Region;
            requireRender = true;
        }

        if (_frozenLeftCount != FrozenLeftCount ||
            _frozenRightCount != FrozenRightCount ||
            _frozenBottomCount != FrozenBottomCount ||
            _frozenTopCount != FrozenTopCount)
        {
            _frozenLeftCount = FrozenLeftCount;
            _frozenRightCount = FrozenRightCount;
            _frozenBottomCount = FrozenBottomCount;
            _frozenTopCount = FrozenTopCount;
            requireRender = true;
        }

        if (_showColHeadings != ShowColHeadings || _showRowHeadings != ShowRowHeadings)
        {
            _showColHeadings = ShowColHeadings;
            _showRowHeadings = ShowRowHeadings;
            requireRender = true;
        }

        if (ShowFormulaDependents != _showFormulaDependents)
        {
            _showFormulaDependents = ShowFormulaDependents;
            requireRender = true;
        }

        if (!MenuOptions.CompareTo(_menuOptions))
        {
            _menuOptions = MenuOptions;
            requireRender = true;
        }

        if (requireRender)
        {
            _sheetIsDirty = true;
            StateHasChanged();
        }

        await base.OnParametersSetAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (GridLevel > 0)
                return;

            _dotnetHelper = DotNetObjectReference.Create(this);

            _sheetPointerInputService = new SheetPointerInputService(Js, _sheetContainer);
            await _sheetPointerInputService.Init();

            _sheetPointerInputService.PointerDown += this.HandleCellMouseDown;
            _sheetPointerInputService.PointerEnter += HandleCellMouseOver;
            _sheetPointerInputService.PointerDoubleClick += HandleCellDoubleClick;

            await AddWindowEventsAsync();
        }

        _renderRequested = false;
        _sheetIsDirty = false;
        _dirtyRows.Clear();

        await base.OnAfterRenderAsync(firstRender);
    }

    private void RemoveEvents(Sheet sheet)
    {
        _sheet.Editor.EditBegin -= EditorOnEditBegin;
        _sheet.Editor.EditFinished -= EditorOnEditFinished;
        _sheet.SheetDirty -= SheetOnSheetDirty;
        _sheet.ScreenUpdatingChanged -= ScreenUpdatingChanged;
    }

    private void AddEvents(Sheet sheet)
    {
        _sheet.Editor.EditBegin += EditorOnEditBegin;
        _sheet.Editor.EditFinished += EditorOnEditFinished;
        _sheet.SheetDirty += SheetOnSheetDirty;
        _sheet.ScreenUpdatingChanged += ScreenUpdatingChanged;
        if (GridLevel == 0)
        {
            _sheet.Rows.Inserted += HandleRowColInserted;
            _sheet.Columns.Inserted += HandleRowColInserted;
            _sheet.Rows.Removed += HandleRowColRemoved;
            _sheet.Columns.Removed += HandleRowColRemoved;
        }

        _sheet.Rows.SizeModified += HandleSizeModified;
        _sheet.Columns.SizeModified += HandleSizeModified;
    }

    private async Task AddWindowEventsAsync()
    {
        await WindowEventService.RegisterMouseEvent("mousedown", HandleWindowMouseDown);
        await WindowEventService.RegisterKeyEvent("keydown", HandleWindowKeyDown);
        await WindowEventService.RegisterClipboardEvent("paste", HandleWindowPaste);
        await WindowEventService.RegisterMouseEvent("mouseup", HandleWindowMouseUp);
    }

    private void HandleRowColInserted(object? sender, RowColInsertedEventArgs? e) => ForceReRender();

    private void HandleRowColRemoved(object? sender, RowColRemovedEventArgs? e) => ForceReRender();

    private void HandleSizeModified(object? sender, SizeModifiedEventArgs e) => ForceReRender();

    /// <summary>
    /// Re-render all cells, regardless of whether they are dirty and refreshes the viewport
    /// </summary>
    public void ForceReRender()
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        _sheetIsDirty = true;
        StateHasChanged();
        RefreshView();
    }

    public async void RefreshView()
    {
        if (_mainView == null)
            return;

        await _mainView.RefreshView();
    }

    private void ScreenUpdatingChanged(object? sender, SheetScreenUpdatingEventArgs e)
    {
        if (e.IsScreenUpdating && _renderRequested)
            this.StateHasChanged();
    }

    private void HandleVirtualViewportChanged(VirtualViewportChangedEventArgs args)
    {
        _currentViewport = args.Viewport;

        foreach (var region in args.RemovedRegions)
        {
            foreach (var position in region)
            {
                _visualCellCache.Remove(position);
            }
        }

        MakeRegionsDirty(args.NewRegions);
    }

    private Dictionary<CellPosition, VisualCell> _visualCellCache = new();

    private void SheetOnSheetDirty(object? sender, DirtySheetEventArgs e)
    {
        var dirtyRegions = e.DirtyRegions
            .GetDataRegions(_currentViewport.ViewRegion)
            .Select(x => x.Region)
            .ToList();

        if (dirtyRegions.Count > 0)
            MakeRegionsDirty(dirtyRegions);
    }

    private void MakeRegionsDirty(IEnumerable<IRegion?> dirtyRegions)
    {
        foreach (var region in dirtyRegions)
        {
            var boundedRegion = region?.GetIntersection(_currentViewport.ViewRegion) as Region;
            if (boundedRegion == null)
                continue;

            _dirtyRows.Add(boundedRegion);

            foreach (var row in _sheet.Rows.GetVisibleIndices(boundedRegion.Top, boundedRegion.Bottom))
            {
                foreach (var col in _sheet.Columns.GetVisibleIndices(boundedRegion.Left, boundedRegion.Right))
                {
                    var position = new CellPosition(row, col);
                    if (!_visualCellCache.TryAdd(position, new VisualCell(row, col, _sheet)))
                    {
                        _visualCellCache[position] = new VisualCell(row, col, _sheet);
                    }
                }
            }
        }

        StateHasChanged();
    }

    private async void EditorOnEditFinished(object? sender, EditFinishedEventArgs e)
    {
        await WindowEventService.PreventDefault("keydown");
    }

    private async void EditorOnEditBegin(object? sender, EditBeginEventArgs e)
    {
        await WindowEventService.CancelPreventDefault("keydown");
    }


    internal async Task<bool> HandleShortcuts(string key, KeyboardModifiers modifiers)
    {
        return await ShortcutManager.ExecuteAsync(key, modifiers,
            new ShortcutExecutionContext(this, _sheet));
    }

    private void RegisterDefaultShortcuts()
    {
        ShortcutManager.Register(["Escape"], KeyboardModifiers.Any,
            _ => _sheet.Editor.CancelEdit());

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
                _ => !_sheet.Editor.IsEditing);

        ShortcutManager
            .Register(["ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft"], KeyboardModifiers.None,
                c =>
                    HandleArrowKeysDown(false, KeyUtil.GetMovementFromArrowKey(c.Key)));

        ShortcutManager
            .Register(["ArrowUp", "ArrowRight", "ArrowDown", "ArrowLeft"], KeyboardModifiers.Shift,
                c =>
                    HandleArrowKeysDown(true, KeyUtil.GetMovementFromArrowKey(c.Key)));

        ShortcutManager.Register(["KeyY"], [KeyboardModifiers.Ctrl, KeyboardModifiers.Meta],
            _ => _sheet.Commands.Redo(),
            _ => !_sheet.Editor.IsEditing);
        ShortcutManager.Register(["KeyZ"], [KeyboardModifiers.Ctrl, KeyboardModifiers.Meta],
            _ => _sheet.Commands.Undo(),
            _ => !_sheet.Editor.IsEditing);

        ShortcutManager.Register(["Delete", "Backspace"], KeyboardModifiers.Any,
            _ => _sheet.Commands.ExecuteCommand(new ClearCellsCommand(_sheet.Selection.Regions)),
            _ => _sheet.Selection.Regions.Any() && !_sheet.Editor.IsEditing);
    }

    private void HandleCellMouseDown(object? sender, SheetPointerEventArgs args)
    {
        // if rmc and inside a selection, don't do anything
        if (args.MouseButton == 2 && _sheet.Selection.Contains(args.Row, args.Col))
            return;


        if (_sheet.Editor.IsEditing)
        {
            if (!(_sheet.Editor.EditCell!.Row == args.Row && _sheet.Editor.EditCell!.Col == args.Col))
            {
                if (!_sheet.Editor.AcceptEdit())
                    return;
            }
        }

        if (args.ShiftKey && _sheet.Selection.ActiveRegion != null)
        {
            _sheet.Selection.ExtendTo(args.Row, args.Col);
        }
        else
        {
            if (!args.MetaKey && !args.CtrlKey)
            {
                _sheet.Selection.ClearSelections();
            }

            if (args.Row == -1)
                _sheet.Selection.BeginSelectingCol(args.Col);
            else if (args.Col == -1)
                _sheet.Selection.BeginSelectingRow(args.Row);
            else
                _sheet.Selection.BeginSelectingCell(args.Row, args.Col);

            if (args.MouseButton == 2) // RMC
                _sheet.Selection.EndSelecting();
        }
    }

    private async Task<bool> HandleWindowKeyDown(KeyboardEventArgs e)
    {
        if (!IsDataSheetActive)
            return false;

        if (MenuService.IsMenuOpen())
            return false;

        var editorHandled = _editorLayer.HandleKeyDown(e.Key, e.CtrlKey, e.ShiftKey, e.AltKey, e.MetaKey);
        if (editorHandled)
            return true;

        var modifiers = e.GetModifiers();
        if (await HandleShortcuts(e.Key, modifiers) || await HandleShortcuts(e.Code, modifiers))
            return true;

        // Single characters or numbers or symbols
        if ((e.Key.Length == 1) && !_sheet.Editor.IsEditing && IsDataSheetActive)
        {
            // Don't input anything if we are currently selecting
            if (_sheet.Selection.IsSelecting)
                return false;

            // Capture commands and return early (mainly for paste)
            if (e.CtrlKey || e.MetaKey)
                return false;

            char c = e.Key == "Space" ? ' ' : e.Key[0];
            if (char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsSeparator(c))
            {
                if (!_sheet.Selection.Regions.Any())
                    return false;
                var inputPosition = _sheet.Selection.GetInputPosition();

                await BeginEdit(inputPosition.row, inputPosition.col, EditEntryMode.Key, e.Key);
            }

            return true;
        }

        return false;
    }

    private async Task<bool> HandleWindowMouseDown(MouseEventArgs e)
    {
        await SetActiveAsync(IsMouseInsideSheet);
        return false;
    }

    private Task<bool> HandleWindowMouseUp(MouseEventArgs arg)
    {
        _sheet.Selection.EndSelecting();
        return Task.FromResult(false);
    }

    private async Task<bool> HandleArrowKeysDown(bool shift, Offset offset)
    {
        var accepted = true;
        if (_sheet.Editor.IsEditing)
            accepted = _sheet.Editor.IsSoftEdit && _sheet.Editor.AcceptEdit();

        if (!accepted) return false;

        if (shift)
        {
            var oldActiveRegion = _sheet.Selection.ActiveRegion?.Clone();
            GrowActiveSelection(offset);
            if (oldActiveRegion == null) return false;
            var r = _sheet.Selection.ActiveRegion!.Break(oldActiveRegion).FirstOrDefault();

            if (r == null)
            {
                // if r is null we are instead shrinking the region, so instead break the old region with the new
                // but we contract the new region to ensure that it is now visible
                var rNew = _sheet.Selection.ActiveRegion.Clone();
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

    private async Task<bool> HandleWindowPaste(ClipboardEventArgs arg)
    {
        if (!IsDataSheetActive)
            return false;

        if (!_sheet.Selection.Regions.Any())
            return false;

        if (_sheet.Editor.IsEditing)
            return false;

        var posnToInput = _sheet.Selection.GetInputPosition();

        var range = _sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return false;

        _sheet.Selection.Set(range);
        return true;
    }

    private async void HandleCellDoubleClick(object? sender, SheetPointerEventArgs args)
    {
        if (args.Row < 0 || args.Col < 0 || args.Row >= _sheet.NumRows || args.Col >= _sheet.NumCols)
            return;

        await BeginEdit(args.Row, args.Col, EditEntryMode.Mouse);
    }

    private async Task BeginEdit(int row, int col, EditEntryMode mode, string entryChar = "")
    {
        if (this.IsReadOnly)
            return;

        _sheet.Selection.CancelSelecting();
        _sheet.Editor.BeginEdit(row, col, mode == EditEntryMode.Key, mode, entryChar);
    }

    private void HandleCellMouseOver(object? sender, SheetPointerEventArgs args)
    {
        _sheet.Selection.UpdateSelectingEndPosition(args.Row, args.Col);
    }

    private async Task<bool> AcceptEditAndMoveActiveSelection(Axis axis, int amount)
    {
        var acceptEdit = !_sheet.Editor.IsEditing || _sheet.Editor.AcceptEdit();
        _sheet.Selection.MoveActivePosition(axis, amount);
        if (IsDataSheetActive)
            await ScrollToActiveCellPosition();
        return acceptEdit;
    }

    private async Task ScrollToActiveCellPosition()
    {
        var cellRect =
            _sheet.Cells.GetMerge(_sheet.Selection.ActiveCellPosition.row, _sheet.Selection.ActiveCellPosition.col) ??
            new Region(_sheet.Selection.ActiveCellPosition.row, _sheet.Selection.ActiveCellPosition.col);
        await ScrollToContainRegion(cellRect);
    }

    private async Task ScrollToContainRegion(IRegion region)
    {
        if (_mainView == null)
            return;

        var frozenLeftW = _sheet.Columns.GetVisualLeft(_frozenLeftCount);
        var frozenRightW = _sheet.Columns.GetVisualWidthBetween(_sheet.NumCols - _frozenRightCount, _sheet.NumCols);
        var frozenTopH = _sheet.Rows.GetVisualTop(_frozenTopCount);
        var frozenBottomH = _sheet.Rows.GetVisualHeightBetween(_sheet.NumRows - _frozenBottomCount, _sheet.NumRows);

        // the viewRect we have from the viewport includes the frozen cols 
        // so we need to consider those when considering whether the region is outside of the view
        var currentViewRect = await _mainView.CalculateViewRect(_sheetContainer);
        var constrainedViewRect = new Rect(
            currentViewRect.X + frozenLeftW,
            currentViewRect.Y + frozenTopH,
            currentViewRect.Width - frozenRightW - frozenLeftW - GetGutterSize(Axis.Row),
            currentViewRect.Height - frozenBottomH - frozenTopH - GetGutterSize(Axis.Col));

        var doScroll = false;
        var regionRect = region.GetRect(_sheet);
        double scrollYDist = 0, scrollXDist = 0;

        // If the region is outside the contained view rect but NOT within the frozen cols
        if ((regionRect.X < constrainedViewRect.X || regionRect.Right > constrainedViewRect.Right) &&
            !(region.Left <= _frozenLeftCount - 1 || region.Right >= _sheet.NumCols - _frozenRightCount))
        {
            var rightDist = regionRect.Right - constrainedViewRect.Right;
            var leftDist = regionRect.X - constrainedViewRect.X;

            scrollXDist = Math.Abs(rightDist) < Math.Abs(leftDist)
                ? rightDist
                : leftDist;

            doScroll = true;
        }

        // If the region is outside the contained view rect but NOT within the frozen rows
        if ((regionRect.Y < constrainedViewRect.Y || regionRect.Bottom > constrainedViewRect.Bottom)
            && !(region.Bottom <= _frozenTopCount - 1 || region.Top >= _sheet.NumRows - _frozenBottomCount))
        {
            var bottomDist = regionRect.Bottom - constrainedViewRect.Bottom;
            var topDist = regionRect.Y - constrainedViewRect.Y;

            scrollYDist = Math.Abs(bottomDist) < Math.Abs(topDist)
                ? bottomDist
                : topDist;

            doScroll = true;
        }

        if (doScroll)
            await _mainView.ScrollBy(scrollXDist, scrollYDist);
    }

    private double GetGutterSize(Axis axis)
    {
        if (axis == Axis.Row && ShowRowHeadings)
            return _sheet.Rows.HeadingWidth;
        if (axis == Axis.Col && ShowColHeadings)
            return _sheet.Columns.HeadingHeight;
        return 0;
    }

    /// <summary>
    /// Increases the size of the active selection, around the active cell position
    /// </summary>
    private void GrowActiveSelection(Offset offset)
    {
        if (_sheet.Selection.ActiveRegion == null)
            return;

        var selPosition = _sheet.Selection.ActiveCellPosition;
        if (offset.Columns != 0)
        {
            if (offset.Columns == -1)
            {
                if (selPosition.col < _sheet.Selection.ActiveRegion.GetEdge(Edge.Right).Right)
                    _sheet.Selection.ContractEdge(Edge.Right, 1);
                else
                    _sheet.Selection.ExpandEdge(Edge.Left, 1);
            }
            else if (offset.Columns == 1)
                if (selPosition.col > _sheet.Selection.ActiveRegion.GetEdge(Edge.Left).Left)
                    _sheet.Selection.ContractEdge(Edge.Left, 1);
                else
                    _sheet.Selection.ExpandEdge(Edge.Right, 1);
        }

        if (offset.Rows != 0)
        {
            if (offset.Rows == -1)
            {
                if (selPosition.row < _sheet.Selection.ActiveRegion.GetEdge(Edge.Bottom).Bottom)
                    _sheet.Selection.ContractEdge(Edge.Bottom, 1);
                else
                    _sheet.Selection.ExpandEdge(Edge.Top, 1);
            }
            else if (offset.Rows == 1)
            {
                if (selPosition.row > _sheet.Selection.ActiveRegion.GetEdge(Edge.Top).Top)
                    _sheet.Selection.ContractEdge(Edge.Top, 1);
                else
                    _sheet.Selection.ExpandEdge(Edge.Bottom, 1);
            }
        }
    }

    private void CollapseAndMoveSelection(Offset offset)
    {
        if (_sheet.Selection.ActiveRegion == null)
            return;

        if (_sheet.Selection.IsSelecting)
            return;

        var posn = _sheet.Selection.ActiveCellPosition;

        _sheet.Selection.Set(posn.row, posn.col);
        _sheet.Selection.MoveActivePositionByRow(offset.Rows);
        _sheet.Selection.MoveActivePositionByCol(offset.Columns);
    }

    /// <summary>
    /// Set the datasheet as active, which controls whether the sheet is ready to receive keyboard input events.
    /// </summary>
    /// <param name="active"></param>
    public async Task SetActiveAsync(bool active = true)
    {
        if (active == IsDataSheetActive)
            return;

        if (active)
            await WindowEventService.PreventDefault("keydown");
        else
            await WindowEventService.CancelPreventDefault("keydown");

        IsDataSheetActive = active;
        await OnSheetActiveChanged.InvokeAsync(new SheetActiveEventArgs(this, active));
    }

    /// <summary>
    /// Copies current selection to clipboard
    /// </summary>
    public async Task<bool> CopySelectionToClipboard()
    {
        if (_sheet.Selection.IsSelecting)
            return false;

        // Can only handle single selections for now
        var region = _sheet.Selection.ActiveRegion;
        if (region == null)
            return false;

        await ClipboardService.Copy(region, _sheet);
        return true;
    }

    /// <summary>
    /// Turn on or off the display of formula dependents
    /// </summary>
    /// <param name="value"></param>
    public void SetShowFormulaDependents(bool value)
    {
        _showFormulaDependents = value;
        ForceReRender();
    }

    private bool IsRowDirty(int rowIndex)
    {
        return _sheetIsDirty || _dirtyRows.Any(x => x.SpansRow(rowIndex));
    }

    protected override bool ShouldRender()
    {
        _renderRequested = true;

        var shouldRender = _sheet.ScreenUpdating && (_sheetIsDirty || _dirtyRows.Count != 0);
        return shouldRender;
    }
}