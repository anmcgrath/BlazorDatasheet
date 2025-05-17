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
using BlazorDatasheet.Render.Layers;
using BlazorDatasheet.Services;
using BlazorDatasheet.Virtualise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using ClipboardEventArgs = BlazorDatasheet.Core.Events.ClipboardEventArgs;
using Microsoft.JSInterop;

[assembly: InternalsVisibleTo("BlazorDatasheet.Test")]

namespace BlazorDatasheet;

public partial class Datasheet : SheetComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime Js { get; set; } = null!;
    private IWindowEventService _windowEventService = null!;
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
    /// When true, the autofill handle will be shown on the bottom right of the cell.
    /// </summary>
    [Parameter]
    public bool UseAutoFill { get; set; } = true;

    private bool _useAutoFill = true;

    /// <summary>
    /// When true, the datasheet will Auto-fit cells when edited or the format is changed.
    /// </summary>
    [Parameter]
    public bool AutoFit { get; set; }

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
    /// The number of decimal places to round a number to. Default is 13.
    /// </summary>
    [Parameter]
    public int NumberPrecisionDisplay { get; set; } = 13;

    private int _numberPrecisionDisplay = 13;

    /// <summary>
    /// Any user-defined items to render in the context menu
    /// </summary>
    [Parameter]
    public RenderFragment<Sheet>? MenuItems { get; set; }

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
    /// The number of columns past the end of the viewport to render.
    /// </summary>
    [Parameter]
    public int OverscanColumns { get; set; } = 2;

    /// <summary>
    /// The number of rows past the end of the viewport to render.
    /// </summary>
    [Parameter]
    public int OverscanRows { get; set; } = 6;

    /// <summary>
    /// Provides menu options for the sheet
    /// </summary>
    [Parameter]
    public SheetMenuOptions MenuOptions { get; set; } = new();

    private SheetMenuOptions _menuOptions = new();

    private DotNetObjectReference<Datasheet>? _dotnetHelper;

    private SheetPointerInputService? _sheetPointerInputService;

    /// <summary>
    /// The whole sheet container, useful for checking whether mouse is inside the sheet
    /// </summary>
    private ElementReference _sheetContainer = default!;

    /// <summary>
    /// Main virtualised view
    /// </summary>
    private Virtualise2D? _mainView;

    private AutofitLayer? _autofitLayer;

    /// <summary>
    /// The editor layer, which renders the cell editor.
    /// </summary>
    private EditorLayer _editorLayer = default!;

    private readonly List<IRegion> _dirtyRegions = new();

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

    public double TotalViewWidth =>
        _sheet.Columns.GetVisualWidth(ViewRegion ?? _sheet.Region) + GetGutterSize(Axis.Row);

    public double TotalViewHeight => _sheet.Rows.GetVisualHeight(ViewRegion ?? _sheet.Region) + GetGutterSize(Axis.Col);

    private SelectionInputManager _selectionManager = default!;

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
        CreateCellRenderFragment();
        ClipboardService = new Clipboard(Js);
        _windowEventService = new WindowEventService(Js);
        RegisterDefaultShortcuts();
        base.OnInitialized();
    }

    protected override async Task OnParametersSetAsync()
    {
        bool requireRender = false;
        bool forceRerender = false;

        if (Sheet != _sheet)
        {
            RemoveEvents(_sheet);
            _sheet = Sheet ?? new(0, 0);
            _selectionManager = new SelectionInputManager(_sheet.Selection);
            AddEvents(_sheet);
            _visualCellCache.Clear();
            requireRender = true;
        }

        if (Theme != _theme)
        {
            _theme = Theme;
            requireRender = true;
        }

        if (!_viewRegion.Equals(ViewRegion))
        {
            _viewRegion = ViewRegion ?? _sheet.Region;
            forceRerender = true;
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

        if (UseAutoFill != _useAutoFill)
        {
            _useAutoFill = UseAutoFill;
            requireRender = true;
        }

        MenuOptions.CustomMenuFragment = MenuItems;
        if (!MenuOptions.CompareTo(_menuOptions))
        {
            _menuOptions = MenuOptions;
            requireRender = true;
        }

        if (NumberPrecisionDisplay != _numberPrecisionDisplay)
        {
            _numberPrecisionDisplay = Math.Min(15, NumberPrecisionDisplay);
        }

        if (forceRerender)
            ForceReRender();
        else if (requireRender)
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

            _sheetPointerInputService.PointerDown += HandleCellMouseDown;
            _sheetPointerInputService.PointerEnter += HandleCellMouseOver;
            _sheetPointerInputService.PointerDoubleClick += HandleCellDoubleClick;

            await AddWindowEventsAsync();
        }

        _renderRequested = false;
        _sheetIsDirty = false;
        _dirtyRegions.Clear();

        await base.OnAfterRenderAsync(firstRender);
    }

    private void RemoveEvents(Sheet sheet)
    {
        sheet.Editor.EditBegin -= EditorOnEditBegin;
        sheet.Editor.EditFinished -= EditorOnEditFinished;
        sheet.SheetDirty -= SheetOnSheetDirty;
        sheet.ScreenUpdatingChanged -= ScreenUpdatingChanged;

        if (GridLevel == 0)
        {
            sheet.Rows.Inserted -= HandleRowColInserted;
            sheet.Columns.Inserted -= HandleRowColInserted;
            sheet.Rows.Removed -= HandleRowColRemoved;
            sheet.Columns.Removed -= HandleRowColRemoved;
        }

        sheet.Rows.SizeModified -= HandleSizeModified;
        sheet.Columns.SizeModified -= HandleSizeModified;
    }

    private void AddEvents(Sheet sheet)
    {
        sheet.Editor.EditBegin += EditorOnEditBegin;
        sheet.Editor.EditFinished += EditorOnEditFinished;
        sheet.SheetDirty += SheetOnSheetDirty;
        sheet.ScreenUpdatingChanged += ScreenUpdatingChanged;
        if (GridLevel == 0)
        {
            sheet.Rows.Inserted += HandleRowColInserted;
            sheet.Columns.Inserted += HandleRowColInserted;
            sheet.Rows.Removed += HandleRowColRemoved;
            sheet.Columns.Removed += HandleRowColRemoved;
        }

        sheet.Rows.SizeModified += HandleSizeModified;
        sheet.Columns.SizeModified += HandleSizeModified;
        sheet.SetDialogService(new SimpleDialogService(Js));
    }

    private async Task AddWindowEventsAsync()
    {
        await _windowEventService.RegisterMouseEvent("mousedown", HandleWindowMouseDown);
        await _windowEventService.RegisterKeyEvent("keydown", HandleWindowKeyDown);
        await _windowEventService.RegisterClipboardEvent("paste", HandleWindowPaste);
        await _windowEventService.RegisterClipboardEvent("copy", HandleWindowCopy);
        await _windowEventService.RegisterMouseEvent("mouseup", HandleWindowMouseUp);
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
        var dirtyRegions = e.DirtyRows
            .GetAllIntervalData()
            .Select(x => new RowRegion(x.interval.Start, x.interval.End))
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

            _dirtyRegions.Add(boundedRegion);

            foreach (var row in _sheet.Rows.GetVisibleIndices(boundedRegion.Top, boundedRegion.Bottom))
            {
                foreach (var col in _sheet.Columns.GetVisibleIndices(boundedRegion.Left, boundedRegion.Right))
                {
                    var position = new CellPosition(row, col);
                    var visualCell = new VisualCell(row, col, _sheet, _numberPrecisionDisplay);

                    if (!_visualCellCache.TryAdd(position, visualCell))
                        _visualCellCache[position] = visualCell;
                }
            }
        }

        StateHasChanged();
    }

    private async void EditorOnEditFinished(object? sender, EditFinishedEventArgs e)
    {
        await _windowEventService.PreventDefault("keydown");
    }

    private async void EditorOnEditBegin(object? sender, EditBeginEventArgs e)
    {
        await _windowEventService.CancelPreventDefault("keydown");
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
            _ => !_sheet.Editor.IsEditing && !IsReadOnly);
        ShortcutManager.Register(["KeyZ"], [KeyboardModifiers.Ctrl, KeyboardModifiers.Meta],
            _ => _sheet.Commands.Undo(),
            _ => !_sheet.Editor.IsEditing && !IsReadOnly);

        ShortcutManager.Register(["Delete", "Backspace"], KeyboardModifiers.Any,
            _ => _sheet.Commands.ExecuteCommand(new ClearCellsCommand(_sheet.Selection.Regions)),
            _ => _sheet.Selection.Regions.Any() && !_sheet.Editor.IsEditing && !IsReadOnly);
    }

    private void HandleCellMouseDown(object? sender, SheetPointerEventArgs args)
    {
        if (_sheet.Editor.IsEditing &&
            _editorLayer.HandleMouseDown(args.Row, args.Col, args.CtrlKey, args.ShiftKey, args.AltKey, args.MetaKey))
            return;

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

        _selectionManager.HandlePointerDown(
            row: args.Row,
            col: args.Col,
            shift: args.ShiftKey,
            ctrl: args.CtrlKey,
            meta: args.MetaKey,
            mouseButton: args.MouseButton);
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
            return false;

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

    private async Task<bool> HandleWindowMouseUp(MouseEventArgs arg)
    {
        if (await _editorLayer.HandleWindowMouseUpAsync())
            return true;

        _selectionManager.HandleWindowMouseUp();
        return false;
    }

    private async Task<bool> HandleArrowKeysDown(bool shift, Offset offset)
    {
        var accepted = true;
        if (_sheet.Editor.IsEditing)
            accepted = _sheet.Editor.IsSoftEdit && _sheet.Editor.AcceptEdit();

        if (!accepted) return false;

        _selectionManager.HandleArrowKeyDown(shift, offset);

        if (!shift && IsDataSheetActive)
            await ScrollToActiveCellPosition();

        return true;
    }

    private Task<bool> HandleWindowPaste(ClipboardEventArgs arg)
    {
        if (!IsDataSheetActive)
            return Task.FromResult(false);

        if (!_sheet.Selection.Regions.Any())
            return Task.FromResult(false);

        if (_sheet.Editor.IsEditing)
            return Task.FromResult(false);

        if (IsReadOnly)
            return Task.FromResult(false);

        var posnToInput = _sheet.Selection.GetInputPosition();

        var range = _sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return Task.FromResult(false);

        _sheet.Selection.Set(range);
        return Task.FromResult(true);
    }

    private async Task<bool> HandleWindowCopy(ClipboardEventArgs arg)
    {
        if (!IsDataSheetActive || _sheet.Editor.IsEditing)
            return false;

        return await CopySelectionToClipboard();
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
        if (_sheet.Editor.IsEditing &&
            _editorLayer.HandleMouseOver(args.Row, args.Col, args.CtrlKey, args.ShiftKey, args.AltKey, args.MetaKey))
            return;

        _selectionManager.HandlePointerOver(args.Row, args.Col);
    }

    private async Task<bool> AcceptEditAndMoveActiveSelection(Axis axis, int amount)
    {
        var acceptEdit = !_sheet.Editor.IsEditing || _sheet.Editor.AcceptEdit();

        if (!acceptEdit)
            return false;

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

    public async Task ScrollToContainRegion(IRegion region)
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
        if (currentViewRect == null)
            return;

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

    public void AutoFitRegion(IRegion region, Axis autoFitAxis, AutofitMethod method)
    {
        _autofitLayer?.AutoFit(region, autoFitAxis, method);
    }

    /// <summary>
    /// Set the datasheet as active, which controls whether the sheet is ready to receive keyboard input events.
    /// </summary>
    /// <param name="active"></param>
    public async Task SetActiveAsync(bool active = true)
    {
        if (active == IsDataSheetActive)
            return;

        if (!_sheet.Editor.IsEditing)
        {
            if (active)
                await _windowEventService.PreventDefault("keydown");
            else
                await _windowEventService.CancelPreventDefault("keydown");
        }

        IsDataSheetActive = active;
        await OnSheetActiveChanged.InvokeAsync(new SheetActiveEventArgs(this, active));
    }


    /// <summary>
    /// Sets the document focus to the sheet container and sets the sheet as active.
    /// </summary>
    public async Task FocusAsync()
    {
        await _sheetContainer.FocusAsync();
        await SetActiveAsync();
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
        return _sheetIsDirty || _dirtyRegions.Any(x => x.SpansRow(rowIndex));
    }

    protected override bool ShouldRender()
    {
        _renderRequested = true;

        var shouldRender = _sheet.ScreenUpdating && (_sheetIsDirty || _dirtyRegions.Count != 0);
        return shouldRender;
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotnetHelper is not null)
            _dotnetHelper.Dispose();

        if (_sheetPointerInputService is not null)
            await _sheetPointerInputService.DisposeAsync();

        await _windowEventService.DisposeAsync();
    }
}