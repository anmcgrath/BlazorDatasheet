using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Render.AutoScroll;
using BlazorDatasheet.Render.Layers.Preview;
using BlazorDatasheet.Services;

namespace BlazorDatasheet.Render;

/// <summary>Owns a selection drag across all panes, without changing cells until release.</summary>
internal sealed class SelectionMoveController : IDisposable
{
    private readonly Sheet _sheet;
    private readonly Func<bool> _isReadOnly;
    private readonly Func<DragMoveKinds> _allowed;
    private readonly PreviewService _previews;
    private readonly AutoScrollState _autoScroll;
    private IRegion? _source;
    private IRegion? _destination;
    private CellPosition _grab;
    private CellPosition _active;
    private double _pageX, _pageY;
    private int _insertion;
    private PreviewItem? _preview;
    private SheetPointerEventArgs? _headingPress;

    public bool IsPending => _source != null;
    public bool IsDragging { get; private set; }
    public bool IsValid { get; private set; }

    /// <summary>Whether the drag was started by pressing on a selected row or column heading.</summary>
    public bool IsHeadingDrag => _headingPress != null;
    public event Action? Changed;

    public bool CanBegin => !_isReadOnly() && !_sheet.Editor.IsEditing &&
        _sheet.Workbook.ActiveFormulaEdit == null && !_sheet.Selection.IsSelecting &&
        _sheet.Selection.Regions.Count() == 1 && _sheet.Selection.ActiveRegion is { } region &&
        _allowed().HasFlag(GetKind(region)) &&
        _sheet.NumRows > 0 && _sheet.NumCols > 0 &&
        DatasheetRegistry.For(_sheet.Workbook).ShowsSelection(_sheet);

    private static DragMoveKinds GetKind(IRegion region) => region switch
    {
        RowRegion => DragMoveKinds.Rows,
        ColumnRegion => DragMoveKinds.Columns,
        _ => DragMoveKinds.Ranges
    };

    public SelectionMoveController(Sheet sheet, Func<bool> isReadOnly, Func<DragMoveKinds> allowed,
        PreviewService previews, AutoScrollState autoScroll)
    {
        _sheet = sheet;
        _isReadOnly = isReadOnly;
        _allowed = allowed;
        _previews = previews;
        _autoScroll = autoScroll;
        sheet.Selection.SelectionChanged += SelectionChanged;
        DatasheetRegistry.For(sheet.Workbook).Changed += CurrentSheetChanged;
    }

    public bool Begin(CellPosition grab, double pageX, double pageY, SheetPointerEventArgs? headingPress = null)
    {
        if (!CanBegin || IsPending)
            return false;
        _source = _sheet.Selection.ActiveRegion!.Clone();
        _active = _sheet.Selection.ActiveCellPosition;
        _grab = grab;
        _pageX = pageX;
        _pageY = pageY;
        _headingPress = headingPress;
        Changed?.Invoke();
        return true;
    }

    public bool BeginHeading(SheetPointerEventArgs args)
    {
        if (args.MouseButton != 0 || args.ShiftKey || args.CtrlKey || args.MetaKey || args.AltKey)
            return false;
        var region = _sheet.Selection.ActiveRegion;
        if (!(region is RowRegion && args.Col == -1 && region.SpansRow(args.Row)) &&
            !(region is ColumnRegion && args.Row == -1 && region.SpansCol(args.Col)))
            return false;
        return Begin(new CellPosition(Math.Max(0, args.Row), Math.Max(0, args.Col)), args.PageX, args.PageY, args);
    }

    public void Move(SheetPointerEventArgs args)
    {
        if (_source == null)
            return;
        if (!CanBegin)
        {
            Cancel();
            return;
        }
        if (!IsDragging && Math.Max(Math.Abs(args.PageX - _pageX), Math.Abs(args.PageY - _pageY)) < 5)
            return;
        var wasDragging = IsDragging;
        var wasValid = IsValid;
        IsDragging = true;
        _autoScroll.SetSelectionMoveActive(true);
        _destination = RegionDragCalculator.Move(_source, _grab,
            new CellPosition(args.Row, args.Col), _sheet.Region);
        PreviewCoord coord = new RegionCoord(_destination);
        if (_source is RowRegion || _source is ColumnRegion)
        {
            var rows = _source is RowRegion;
            var size = rows ? _sheet.NumRows : _sheet.NumCols;
            var index = rows ? args.Row : args.Col;
            var fraction = rows ? args.RowFraction : args.ColFraction;
            _insertion = Math.Clamp(index + (fraction >= .5 ? 1 : 0), 0, size);
            var start = rows ? _source.Top : _source.Left;
            var count = rows ? _source.Height : _source.Width;
            var final = _insertion > start ? _insertion - count : _insertion;
            _destination = rows ? new RowRegion(Math.Max(0, final), Math.Max(0, final) + count - 1)
                : new ColumnRegion(Math.Max(0, final), Math.Max(0, final) + count - 1);
            coord = rows
                ? new PixelCoord(0, _sheet.Rows.GetVisualTop(_insertion), _sheet.Columns.GetVisualLeft(_sheet.NumCols), 0)
                : new PixelCoord(_sheet.Columns.GetVisualLeft(_insertion), 0, 0, _sheet.Rows.GetVisualTop(_sheet.NumRows));
        }
        IsValid = args.IsInsideSheet && CreateCommand().CanExecute(_sheet);
        var next = new PreviewItem
        {
            Coord = coord, BorderStyle = "dashed", BorderThickness = "2px", ZIndex = 15,
            BorderColor = IsValid ? "var(--selection-border-color)" : "var(--invalid-cell-foreground, #c42b1c)"
        };
        _previews.Update(_preview, next);
        _preview = next;
        if (!wasDragging || wasValid != IsValid)
            Changed?.Invoke();
    }

    private (CellPosition To, Axis? ShiftAxis) GetMoveTarget() => _source switch
    {
        RowRegion => (new CellPosition(_insertion, 0), Axis.Row),
        ColumnRegion => (new CellPosition(0, _insertion), Axis.Col),
        _ => (_destination == null ? default : new CellPosition(_destination.Top, _destination.Left), null)
    };

    private ICommand CreateCommand()
    {
        var from = _sheet.Range(_source!);
        if (_source is RowRegion)
            return new MoveRangeWithShiftCommand(from, _sheet.Range(new RowRegion(_insertion)), Axis.Row);
        if (_source is ColumnRegion)
            return new MoveRangeWithShiftCommand(from, _sheet.Range(new ColumnRegion(_insertion)), Axis.Col);
        return new MoveRangeCommand(from, _sheet.Range(_destination!));
    }

    /// <returns>The deferred heading click, if the pointer never crossed the drag threshold.</returns>
    public SheetPointerEventArgs? End(SheetPointerEventArgs args)
    {
        if (!IsPending)
            return null;
        if (args.IsCancelled || !args.IsInsideSheet)
        {
            Cancel();
            return null;
        }
        Move(args);
        if (!IsPending)
            return null;
        var click = !IsDragging ? _headingPress : null;
        var source = _source!;
        var active = _active;
        var commit = IsDragging && IsValid;
        var (to, shiftAxis) = GetMoveTarget();
        // Detach the drag before the move emits its selection/layout events.
        Cancel();
        if (!commit)
            return click;
        // the sheet's BeforeRangeMove handlers may cancel the move or send it somewhere else
        var destination = _sheet.Cells.MoveImpl(_sheet.Range(source), to, shiftAxis, isUserDrag: true);
        if (destination != null)
        {
            _sheet.Selection.Set(destination);
            _sheet.Selection.Activate(active.row + destination.Top - source.Top,
                active.col + destination.Left - source.Left);
        }
        return click;
    }

    public void Cancel()
    {
        if (!IsPending)
            return;
        _source = null;
        _destination = null;
        _headingPress = null;
        IsDragging = IsValid = false;
        if (_preview != null)
            _previews.Remove(_preview);
        _preview = null;
        _autoScroll.SetSelectionMoveActive(false);
        Changed?.Invoke();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs args) => Cancel();
    private void CurrentSheetChanged()
    {
        if (!DatasheetRegistry.For(_sheet.Workbook).ShowsSelection(_sheet))
            Cancel();
    }

    public void Dispose()
    {
        Cancel();
        _sheet.Selection.SelectionChanged -= SelectionChanged;
        DatasheetRegistry.For(_sheet.Workbook).Changed -= CurrentSheetChanged;
    }
}
