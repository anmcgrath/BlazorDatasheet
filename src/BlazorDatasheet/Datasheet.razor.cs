using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Events;
using BlazorDatasheet.Events.Layout;
using BlazorDatasheet.Formats;
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
using Microsoft.JSInterop;

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

    /// <summary>
    /// Whether the user is focused on the datasheet.
    /// </summary>
    private bool IsDataSheetActive { get; set; }

    /// <summary>
    /// Whether the mouse is located inside/over the sheet.
    /// </summary>
    private bool IsMouseInsideSheet { get; set; }

    /// <summary>
    /// The current (or close to) region in view.
    /// </summary>
    public IRegion? ViewportRegion { get; private set; }

    /// <summary>
    /// The total height of the VISIBLE sheet. This changes when the user scrolls or the parent scroll element is resized.
    /// </summary>
    public double RenderedInnerSheetHeight => Sheet!.LayoutProvider.ComputeHeight(RowStart, NVisibleRows);

    /// <summary>
    /// The total width of the VISIBLE sheet. This changes when the user scrolls or the parent scroll element is resized.
    /// </summary>
    public double RenderedInnerSheetWidth => Sheet!.LayoutProvider.ComputeWidth(ColStart, NVisibleCols);

    /// <summary>
    /// Store any cells that are dirty here
    /// </summary>
    private HashSet<(int row, int col)> DirtyCells { get; set; } = new();

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
    /// Whether the entire sheet is dirty
    /// </summary>
    public bool SheetIsDirty { get; set; } = true;

    /// <summary>
    /// Whether the user is actively selecting cells/rows/columns in the sheet.
    /// </summary>
    internal bool IsSelecting => Sheet != null && Sheet.Selection.IsSelecting;

    /// <summary>
    /// Manages the display of the editor, which is rendered using absolute coordinates over the top of the sheet.
    /// </summary>
    private EditorOverlayRenderer _editorManager;

    /// <summary>
    /// Mouse/keyboard window events registration/handling.
    /// </summary>
    private IWindowEventService _windowEventService;

    /// <summary>
    /// Clipboard service that provides copy/paste functionality.
    /// </summary>
    private IClipboard _clipboard;

    // This ensures that the sheet is not re-rendered when mouse events are handled inside the sheet.
    // Performance is improved dramatically when this is used.
    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => callback.InvokeAsync(arg);

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
            // Remove any listeners on the old sheet
            RemoveSheetEventListeners();

            _sheetLocal = Sheet;
            _sheetLocal?.SetDialogService(new SimpleDialogService(this.JS));

            AddSheetEventListeners();
        }

        base.OnParametersSet();
    }

    private void RemoveSheetEventListeners()
    {
        if (_sheetLocal == null) return;
        _sheetLocal.RowInserted -= SheetOnRowInserted;
        _sheetLocal.RowRemoved -= SheetOnRowRemoved;
        _sheetLocal.ColumnInserted -= SheetOnColInserted;
        _sheetLocal.ColumnRemoved -= SheetOnColRemoved;
        _sheetLocal.FormatsChanged -= SheetLocalOnFormatsChanged;
        _sheetLocal.ColumnWidthChanged -= SheetLocalOnColumnWidthChanged;
        _sheetLocal.SheetInvalidated -= SheetLocalOnSheetInvalidated;
        _sheetLocal.Merges.RegionMerged -= SheetLocalOnRegionMerged;
        _sheetLocal.Merges.RegionUnMerged -= SheetLocalOnRegionUnMerged;
    }

    private void AddSheetEventListeners()
    {
        if (_sheetLocal == null) return;
        _sheetLocal.RowInserted += SheetOnRowInserted;
        _sheetLocal.RowRemoved += SheetOnRowRemoved;
        _sheetLocal.ColumnInserted += SheetOnColInserted;
        _sheetLocal.ColumnRemoved += SheetOnColRemoved;
        _sheetLocal.FormatsChanged += SheetLocalOnFormatsChanged;
        _sheetLocal.ColumnWidthChanged += SheetLocalOnColumnWidthChanged;
        _sheetLocal.SheetInvalidated += SheetLocalOnSheetInvalidated;
        _sheetLocal.CellsChanged += SheetLocalOnCellsChanged;
        _sheetLocal.Merges.RegionMerged += SheetLocalOnRegionMerged;
        _sheetLocal.Merges.RegionUnMerged += SheetLocalOnRegionUnMerged;
    }

    private void RegisterDefaultCellRendererAndEditors()
    {
        _defaultCellTypeDefinitions.Add("text", CellTypeDefinition.Create<TextEditorComponent, TextRenderer>());
        _defaultCellTypeDefinitions.Add("datetime", CellTypeDefinition.Create<DateTimeEditorComponent, TextRenderer>());
        _defaultCellTypeDefinitions.Add("boolean", CellTypeDefinition.Create<TextEditorComponent, BoolRenderer>());
        _defaultCellTypeDefinitions.Add("select", CellTypeDefinition.Create<SelectEditorComponent, SelectRenderer>());
        _defaultCellTypeDefinitions.Add("textarea", CellTypeDefinition.Create<TextareaEditorComponent, TextRenderer>());
    }

    private void SheetLocalOnRegionUnMerged(object? sender, IRegion region)
    {
        this.MarkDirty(region);
        this.StateHasChanged();
    }

    private void SheetLocalOnRegionMerged(object? sender, IRegion region)
    {
        this.MarkDirty(region);
        this.StateHasChanged();
    }

    private void MarkDirty(IRegion region)
    {
        // constrain to 
        var constrainedRegion = Sheet.Region.GetIntersection(region);
        var posns = Sheet.Range(constrainedRegion).Positions;

        var ct = posns.Count();
        foreach (var posn in posns)
        {
            MarkDirty(posn.row, posn.col);
        }
    }

    /// <summary>
    /// Marks a cell as "Dirty" so that it will be re-rendered when the Datasheet's state has changed.
    /// </summary>
    /// <param name="row">The dirty cell's row</param>
    /// <param name="col">The dirty cell's column.</param>
    private void MarkDirty(int row, int col)
    {
        DirtyCells.Add((row, col));
    }

    private void SheetLocalOnCellsChanged(object? sender, IEnumerable<Events.ChangeEventArgs> e)
    {
        foreach (var change in e)
            MarkDirty(change.Row, change.Col);
        StateHasChanged();
    }

    private void SheetOnColInserted(object? sender, ColumnInsertedEventArgs e)
    {
        this.ForceReRender();
    }

    private void SheetOnColRemoved(object? sender, ColumnRemovedEventArgs e)
    {
        this.ForceReRender();
    }

    private void SheetLocalOnSheetInvalidated(object? sender, SheetInvalidateEventArgs e)
    {
        DirtyCells = e.DirtyCells.ToHashSet();
        StateHasChanged();
    }

    private void SheetLocalOnColumnWidthChanged(object? sender, ColumnWidthChangedEventArgs e)
    {
        this.ForceReRender();
    }

    private void SheetLocalOnFormatsChanged(object? sender, FormatChangedEventArgs e)
    {
        this.ForceReRender();
    }

    private async void SheetOnRowRemoved(object? sender, RowRemovedEventArgs e)
    {
        this.ForceReRender();
    }

    private async void SheetOnRowInserted(object? sender, RowInsertedEventArgs e)
    {
        this.ForceReRender();
    }

    private Type getCellRendererType(string type)
    {
        // First look at any custom renderers
        if (CustomCellTypeDefinitions.ContainsKey(type))
            return CustomCellTypeDefinitions[type].RendererType;

        if (_defaultCellTypeDefinitions.ContainsKey(type))
            return _defaultCellTypeDefinitions[type].RendererType;

        return typeof(TextRenderer);
    }

    private Dictionary<string, object> getCellRendererParameters(Sheet sheet, IReadOnlyCell cell, int row, int col)
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
            await JS.InvokeVoidAsync("addVirtualisationHandlers",
                                     _dotnetHelper,
                                     _wholeSheetDiv,
                                     nameof(HandleScroll),
                                     _fillerLeft1,
                                     _fillerTop,
                                     _fillerRight,
                                     _fillerBottom);

            // we need to know the position of the datasheet relative to the 
            // page start, so that we can calculate mouse events correctly
            //await JS.InvokeVoidAsync("addSheetPositionHandler", _dotnetHelper, _wholeSheetDiv);
        }

        SheetIsDirty = false;
        DirtyCells.Clear();
    }

    private ScrollEvent ScrollEvent = new ScrollEvent();

    private int RowStart { get; set; }
    private int ColStart { get; set; }
    private int NVisibleRows { get; set; }
    private int NVisibleCols { get; set; }

    /// <summary>
    /// Handles the JS scroll event. Returns the Rectangle that contains information about how much
    /// the viewport has to move before it will have to re-render the cells.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    [JSInvokable("HandleScroll")]
    public void HandleScroll(ScrollEvent e)
    {
        var sw = new Stopwatch();
        sw.Start();
        ScrollEvent = e;

        int overflowY = 6;
        int overflowX = 2;

        var visibleRowStart = Sheet.LayoutProvider.ComputeRow(e.ScrollTop);
        var visibleColStart = Sheet.LayoutProvider.ComputeColumn(e.ScrollLeft);
        var visibleRowEnd = Sheet.LayoutProvider.ComputeRow(e.ScrollTop + e.ContainerHeight);
        var visibleColEnd = Sheet.LayoutProvider.ComputeColumn(e.ScrollLeft + e.ContainerWidth);

        visibleRowEnd = Math.Min(Sheet.NumRows - 1, visibleRowEnd);
        visibleColEnd = Math.Min(Sheet.NumCols - 1, visibleColEnd);

        RowStart = Math.Max(visibleRowStart - overflowY, 0);
        var endRow = Math.Min(Sheet.NumRows - 1, visibleRowEnd + overflowY);
        NVisibleRows = endRow - RowStart + 1;

        ColStart = Math.Max(visibleColStart - overflowX, 0);
        var endCol = Math.Min(Sheet.NumCols - 1, visibleColEnd + overflowX);
        NVisibleCols = endCol - ColStart + 1;

        var prevRegion = ViewportRegion?.Copy();

        this.ViewportRegion = new Region(RowStart, endRow, ColStart, endCol);

        if (prevRegion != null)
        {
            if (prevRegion.Contains(this.ViewportRegion))
                return;

            var newRegions = this.ViewportRegion.Break(prevRegion);
            foreach (var region in newRegions)
                this.MarkDirty(region);
        }

        sw.Reset();
        this.StateHasChanged();
    }

    private string GetAbsoluteCellPositionStyles(int row, int col, int rowSpan, int colSpan)
    {
        var sb = new StringBuilder();
        var top = Sheet.LayoutProvider.ComputeTopPosition(row) +
                  (_sheetLocal.ShowColumnHeadings ? Sheet.LayoutProvider.ColHeadingHeight : 0);
        var left = Sheet.LayoutProvider.ComputeLeftPosition(col) +
                   (_sheetLocal.ShowRowHeadings ? Sheet.LayoutProvider.RowHeadingWidth : 0);
        sb.Append("position:absolute;");
        sb.Append($"top:{top + 1}px;");
        sb.Append($"width:{Sheet.LayoutProvider.ComputeWidth(col, colSpan) - 1}px;");
        sb.Append($"height:{Sheet.LayoutProvider.ComputeHeight(row, rowSpan) - 1}px;");
        sb.Append($"left:{left + 1}px;");
        return sb.ToString();
    }

    private async Task AddWindowEventsAsync()
    {
        await _windowEventService.Init();
        _windowEventService.OnKeyDown += HandleWindowKeyDown;
        _windowEventService.OnMouseDown += HandleWindowMouseDown;
        _windowEventService.OnPaste += HandleWindowPaste;
    }

    private void HandleCellMouseUp(int row, int col, bool MetaKey, bool CtrlKey, bool ShiftKey)
    {
        this.EndSelecting();
    }

    private void HandleCellMouseDown(int row, int col, bool MetaKey, bool CtrlKey, bool ShiftKey)
    {
        if (ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ExtendTo(row, col);
        else
        {
            if (!MetaKey && !CtrlKey)
            {
                Sheet?.Selection?.ClearSelections();
            }

            var mergeRangeAtPosition = _sheetLocal.Merges.Get(row, col);
            this.BeginSelectingCell(row, col);
        }

        if (_sheetLocal.Editor.IsEditing)
        {
            if (!(Sheet.Editor.EditCell.Row == row && Sheet.Editor.EditCell.Col == col))
                AcceptEdit();
        }
    }

    private void HandleColumnHeaderMouseDown(int col, MouseEventArgs e)
    {
        if (e.ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ExtendTo(0, col);
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
        Console.WriteLine("HandleRowHeaderMouseDown " + row);
        if (e.ShiftKey && Sheet?.Selection?.ActiveRegion != null)
            Sheet?.Selection?.ExtendTo(row, 0);
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

    private async void HandleCellDoubleClick(int row, int col, bool MetaKey, bool CtrlKey, bool ShiftKey)
    {
        await BeginEdit(row, col, softEdit: false, EditEntryMode.Mouse);
    }

    private async Task BeginEdit(int row, int col, bool softEdit, EditEntryMode mode, string entryChar = "")
    {
        if (this.IsReadOnly)
            return;

        this.CancelSelecting();

        var cell = Sheet?.GetCell(row, col);
        if (cell == null || cell?.Formatting?.IsReadOnly == true)
            return;

        Sheet.Editor.BeginEdit(row, col, softEdit, mode, entryChar);
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
            if (!Sheet.Editor.IsEditing || this.AcceptEdit())
            {
                var movementDir = e.ShiftKey ? -1 : 1;
                Sheet?.Selection?.MoveActivePositionByRow(movementDir);
                return true;
            }
        }

        if (KeyUtil.IsArrowKey(e.Key))
        {
            var direction = KeyUtil.GetKeyMovementDirection(e.Key);
            if (!Sheet.Editor.IsEditing || (_editorManager.IsSoftEdit && AcceptEdit()))
            {
                this.collapseAndMoveSelection(direction.Item1, direction.Item2);
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
            CopySelectionToClipboard();
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

            Sheet.Selection.ClearCells();
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
                if (inputPosition.IsInvalid)
                    return false;
                BeginEdit(inputPosition.Row, inputPosition.Col, softEdit: true, EditEntryMode.Key, e.Key);
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
            if (inputPosition.IsInvalid)
                return false;
            BeginEdit(inputPosition.Row, inputPosition.Col, softEdit: true, EditEntryMode.Key, e.Key);

            return true;
        }

        return false;
    }

    private void collapseAndMoveSelection(int drow, int dcol)
    {
        if (Sheet?.Selection?.ActiveRegion == null)
            return;

        if (Sheet?.Selection.IsSelecting == true)
            return;

        var posn = Sheet.Selection.ActiveCellPosition;
        Sheet.Selection.ClearSelections();
        Sheet.Selection.SetSingle(posn.Row, posn.Col);
        Sheet.Selection.MoveActivePositionByRow(drow);
        Sheet.Selection.MoveActivePositionByCol(dcol);
    }

    private async Task HandleWindowPaste(PasteEventArgs arg)
    {
        if (!IsDataSheetActive)
            return;

        if (Sheet == null || !Sheet.Selection.Regions.Any())
            return;

        var posnToInput = Sheet.Selection.GetInputPosition();
        if (posnToInput.IsInvalid)
            return;

        var range = Sheet.InsertDelimitedText(arg.Text, posnToInput);
        if (range == null)
            return;

        Sheet.Selection.SetSingle(range);
    }

    public async void Dispose()
    {
        try
        {
            await JS.InvokeAsync<string>("disposeVirtualisationHandlers", _wholeSheetDiv);
            await JS.InvokeAsync<string>("disposePageMoveListener", _wholeSheetDiv);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }

        _windowEventService.Dispose();
        _dotnetHelper?.Dispose();
    }

    /// <summary>
    /// Handles when a cell renderer requests to start editing the cell
    /// </summary>
    /// <param name="args"></param>
    private async void HandleCellRequestBeginEdit(CellEditRequest args)
    {
        await BeginEdit(args.Row, args.Col, args.IsSoftEdit, args.EntryMode);
    }

    /// <summary>
    /// Handles when a cell renderer requests that a cell's value be changed
    /// </summary>
    /// <param name="args"></param>
    private void HandleCellRendererRequestChangeValue(ChangeCellValueRequest args)
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

        if (Sheet?.Selection?.SelectingRegion?.End.Row == row &&
            Sheet?.Selection?.SelectingRegion?.End.Col == col)
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

    /// <summary>
    /// Determines whether a column contains any cells that are selected or being selected
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    private bool IsColumnActive(int col)
    {
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
        if (Sheet?.Selection.Regions.Any(x => x.SpansRow(row)) == true)
            return true;
        return false;
    }

    /// <summary>
    /// Returns the width of the sheet(including the headings, if any) in pixels
    /// </summary>
    /// <returns></returns>
    private double GetSheetWidthInPx()
    {
        var columnWidth = _sheetLocal.LayoutProvider.TotalWidth;
        var headingWidth = _sheetLocal.ShowRowHeadings ? _sheetLocal.ColumnInfo.DefaultWidth : 0;
        return columnWidth + headingWidth;
    }

    /// <summary>
    /// Returns the width of the sheet(including the headings, if any) in pixels
    /// </summary>
    /// <returns></returns>
    private double GetSheetHeightInPx()
    {
        var rowHeights = _sheetLocal.LayoutProvider.TotalHeight;
        var headingWidth = _sheetLocal.ShowColumnHeadings ? _sheetLocal.RowInfo.DefaultHeight : 0;
        return rowHeights + headingWidth;
    }

    private string GetContainerStyleString()
    {
        var sb = new StringBuilder();
        sb.Append($"height:{GetSheetHeightInPx()}px;");
        sb.Append($"width:{GetSheetWidthInPx()}px;");
        return sb.ToString();
    }

    private string GetContainerClassString()
    {
        var sb = new StringBuilder();
        sb.Append(" vars sheet ");
        sb.Append(IsDataSheetActive ? " active-sheet " : " in-active-sheet ");
        return sb.ToString();
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

        this.ViewportRegion = new Region(startIndex, startIndex + numRows, 0, _sheetLocal.NumCols);

        ForceReRender();

        return new ItemsProviderResult<int>(Enumerable.Range(startIndex, numRows), Sheet.NumRows);
    }

    private (int row, int col) RowColFromMouseEvent(MouseEventArgs obj)
    {
        var offsetX = obj.PageX;
        var offsetY = obj.PageY;

        offsetX = _sheetLocal.ShowRowHeadings
            ? offsetX - _sheetLocal.LayoutProvider.RowHeadingWidth
            : offsetX;
        offsetY = _sheetLocal.ShowColumnHeadings
            ? offsetY - _sheetLocal.LayoutProvider.ColHeadingHeight
            : offsetY;
        
        Console.WriteLine(obj.OffsetX);

        var colStartX = Sheet.LayoutProvider.ComputeLeftPosition(ColStart);
        var rowStartY = Sheet.LayoutProvider.ComputeTopPosition(RowStart);
        var col = Sheet.LayoutProvider.ComputeColumn(offsetX + colStartX);
        var row = Sheet.LayoutProvider.ComputeRow(offsetY + rowStartY);

        // check for any merged cells, if so then redirect the row/col to the merge start
        var merged = Sheet.Merges.Get(row, col);
        if (merged != null)
        {
            row = merged.Top;
            col = merged.Left;
        }

        return (row, col);
    }

    private void SheetMouseDown(MouseEventArgs obj)
    {
        var (row, col) = RowColFromMouseEvent(obj);
        this.HandleCellMouseDown(row, col, obj.MetaKey, obj.CtrlKey, obj.ShiftKey);
    }

    private void SheetMouseUp(MouseEventArgs obj)
    {
        var (row, col) = RowColFromMouseEvent(obj);
        this.HandleCellMouseUp(row, col, obj.MetaKey, obj.CtrlKey, obj.ShiftKey);
    }

    private void SheetMouseMove(MouseEventArgs obj)
    {
        var (row, col) = RowColFromMouseEvent(obj);
        this.HandleCellMouseOver(row, col);
    }

    private void SheetMouseDoubleClick(MouseEventArgs obj)
    {
        var (row, col) = RowColFromMouseEvent(obj);
        this.HandleCellDoubleClick(row, col, obj.MetaKey, obj.CtrlKey, obj.ShiftKey);
    }

    /// <summary>
    /// Re-render all cells, regardless of whether they are dirty
    /// </summary>
    public void ForceReRender()
    {
        SheetIsDirty = true;
        StateHasChanged();
    }
}