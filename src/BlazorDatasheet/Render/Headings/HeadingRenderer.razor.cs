using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render.Layout;
using BlazorDatasheet.Util;
using BlazorDatasheet.Services;
using BlazorDatasheet.Virtualise;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Render.Headings;

public partial class HeadingRenderer : SheetComponentBase, IDisposable
{
    [Parameter, EditorRequired] public Sheet? Sheet { get; set; }
    [Parameter] public Region? ViewRegion { get; set; }
    [Parameter, EditorRequired] public RenderFragment<HeadingContext> ChildContent { get; set; } = null!;

    /// <summary>
    /// Whether the selection is shown while the user is working in another sheet of the workbook.
    /// </summary>
    [Parameter]
    public bool ShowSelectionWhenNotCurrentSheet { get; set; }

    /// <summary>
    /// Whether the headings show which rows or columns are selected.
    /// </summary>
    protected bool ShowsSelection =>
        ShowSelectionWhenNotCurrentSheet || DatasheetRegistry.For(_sheet.Workbook).ShowsSelection(_sheet);

    private WorkbookViews? _workbookViews;

    protected Virtualise2D? MainView;

    protected Region _viewRegion = new(-1, -1);
    protected Sheet _sheet = EmptySheet.Instance;
    protected Axis Axis { get; }
    protected IGridLayoutProvider LayoutProvider { get; private set; } = new EmptyLayoutProvider();

    public HeadingRenderer(Axis axis)
    {
        Axis = axis;
    }

    protected override async Task OnParametersSetAsync()
    {
        var refreshView = false;

        if (Sheet != _sheet)
        {
            UnSubscribeEvents(_sheet);

            _sheet = Sheet ?? new(0, 0);
            SubscribeEvents(_sheet);

            LayoutProvider = Axis == Axis.Col
                ? new ColHeadingLayoutProvider(_sheet)
                : new RowHeadingLayoutProvider(_sheet);
            LayoutProvider.ViewRegion = _viewRegion;
            InvalidateHeadingRegions();

            refreshView = true;
        }

        if (ViewRegion != _viewRegion)
        {
            _viewRegion = ViewRegion ?? _sheet.Region;
            LayoutProvider.ViewRegion = _viewRegion;
            InvalidateHeadingRegions();
            refreshView = true;
        }

        // any parameter change (ChildContent, ShowColumnMenu, ...) has to reach the markup, and
        // ShouldRender gates everything on _dirty.
        _dirty = true;

        if (refreshView)
            await RefreshView();
    }

    private void UnSubscribeEvents(Sheet sheet)
    {
        sheet.Selection.SelectionChanged -= SelectionChanged;
        sheet.Selection.SelectingChanged -= SelectingChanged;
        sheet.Rows.SizeModified -= HandleSizeModified;
        sheet.Rows.Inserted -= HandleRowColInserted;
        sheet.Columns.Inserted -= HandleRowColInserted;
        sheet.Rows.Removed -= HandleRowColRemoved;
        sheet.Columns.Removed -= HandleRowColRemoved;
        sheet.Columns.SizeModified -= HandleSizeModified;
        sheet.Rows.HeadingsModified -= HandleHeadingsModified;
        sheet.Columns.HeadingsModified -= HandleHeadingsModified;
        sheet.Rows.GroupsModified -= HandleGroupsModified;
        sheet.Columns.GroupsModified -= HandleGroupsModified;
        sheet.Protection.Changed -= HandleProtectionChanged;
        sheet.FrozenRowCols -= HandleFrozenRowCols;

        if (_workbookViews != null)
            _workbookViews.Changed -= CurrentSheetChanged;
        _workbookViews = null;
    }

    private void SubscribeEvents(Sheet sheet)
    {
        sheet.Selection.SelectionChanged += SelectionChanged;
        sheet.Selection.SelectingChanged += SelectingChanged;
        sheet.Rows.SizeModified += HandleSizeModified;
        sheet.Rows.Inserted += HandleRowColInserted;
        sheet.Columns.Inserted += HandleRowColInserted;
        sheet.Rows.Removed += HandleRowColRemoved;
        sheet.Columns.Removed += HandleRowColRemoved;
        sheet.Columns.SizeModified += HandleSizeModified;
        sheet.Rows.HeadingsModified += HandleHeadingsModified;
        sheet.Columns.HeadingsModified += HandleHeadingsModified;
        sheet.Rows.GroupsModified += HandleGroupsModified;
        sheet.Columns.GroupsModified += HandleGroupsModified;
        sheet.Protection.Changed += HandleProtectionChanged;
        sheet.FrozenRowCols += HandleFrozenRowCols;

        _workbookViews = DatasheetRegistry.For(sheet.Workbook);
        _workbookViews.Changed += CurrentSheetChanged;
    }

    private void CurrentSheetChanged() => _ = InvokeAsync(() =>
    {
        _dirty = true;
        StateHasChanged();
    });

    private void HandleHeadingsModified(object? sender, HeadingsModifiedEventArgs e)
    {
        if (e.Axis != Axis)
            return;

        _dirty = true;
        StateHasChanged();
    }

    private void HandleGroupsModified(object? sender, HeadingGroupsModifiedEventArgs e)
    {
        if (e.Axis != Axis)
            return;

        _dirty = true;
        StateHasChanged();
    }

    private void HandleProtectionChanged(object? sender, EventArgs e)
    {
        _dirty = true;
        _ = InvokeAsync(StateHasChanged);
    }

    private void HandleFrozenRowCols(object? sender, SheetFrozenRowColsEventArgs e)
    {
        // the frozen heading panes are gated on the freeze state, so they must re-render with it
        InvalidateHeadingRegions();
        _dirty = true;
        StateHasChanged();
    }

    /// <summary>
    /// Clears any cached heading regions. They depend on the view region, the freeze state and the
    /// number of rows/columns, so they are invalidated whenever one of those changes.
    /// </summary>
    protected virtual void InvalidateHeadingRegions()
    {
    }

    private bool _dirty;

    /// <summary>
    /// Describes which headings in the view region are painted as selected. Recomputed after every
    /// render, so it always describes what is on screen.
    /// </summary>
    private int _selectionSignature;

    /// <summary>
    /// A compact description of the selected headings on this axis. Headings only show whether an
    /// index is selected, and whether it is selected by a whole row/column region, so a selection
    /// change that leaves both unchanged - the <c>SelectingChanged(null)</c> on the mouse up after a
    /// plain click, for instance - does not need a render.
    /// </summary>
    private int ComputeSelectionSignature()
    {
        if (!ShowsSelection)
            return 0;

        var hash = new HashCode();
        hash.Add(1); // distinguishes "nothing selected" from "selection not shown"

        foreach (var region in _sheet.Selection.Regions)
            AddToSelectionSignature(ref hash, region);

        AddToSelectionSignature(ref hash, _sheet.Selection.SelectingRegion);

        return hash.ToHashCode();
    }

    private void AddToSelectionSignature(ref HashCode hash, IRegion? region)
    {
        if (region == null)
            return;

        var (viewStart, viewEnd) = Axis == Axis.Col
            ? (_viewRegion.Left, _viewRegion.Right)
            : (_viewRegion.Top, _viewRegion.Bottom);
        var (start, end) = Axis == Axis.Col
            ? (region.Left, region.Right)
            : (region.Top, region.Bottom);

        if (end < viewStart || start > viewEnd)
            return;

        hash.Add(Math.Max(start, viewStart));
        hash.Add(Math.Min(end, viewEnd));
        hash.Add(Axis == Axis.Col ? region is ColumnRegion : region is RowRegion);
    }

    private void SelectingChanged(object? sender, IRegion? e) => SelectionPainted();

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e) => SelectionPainted();

    private void SelectionPainted()
    {
        if (ComputeSelectionSignature() == _selectionSignature)
            return;

        _dirty = true;
        StateHasChanged();
    }

    protected override bool ShouldRender()
    {
        if (!_dirty) return false;
        _dirty = false;
        _selectionSignature = ComputeSelectionSignature();
        return true;
    }

    private void HandleRowColInserted(object? sender, RowColInsertedEventArgs? e)
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        InvalidateHeadingRegions();
        _dirty = true;
        StateHasChanged();
    }

    private void HandleRowColRemoved(object? sender, RowColRemovedEventArgs? e)
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        InvalidateHeadingRegions();
        _dirty = true;
        StateHasChanged();
    }

    private async void HandleSizeModified(object? sender, SizeModifiedEventArgs e)
    {
        _dirty = true;
        StateHasChanged();
        await RefreshView();
    }

    public async Task RefreshView()
    {
        if (MainView is not null)
        {
            await MainView.RefreshView();
        }

        await RefreshAdditionalViews();
    }

    protected virtual Task RefreshAdditionalViews() => Task.CompletedTask;

    public virtual void Dispose()
    {
        UnSubscribeEvents(_sheet);
    }

    protected string GetSelectedClass(int index)
    {
        if (!ShowsSelection)
            return string.Empty;

        bool isAxisRegion = false;
        bool isSelected = false;

        foreach (var selection in _sheet.Selection.Regions)
        {
            if (selection.Spans(index, Axis))
            {
                isSelected = true;
                if (Axis == Axis.Col && selection is ColumnRegion ||
                    Axis == Axis.Row && selection is RowRegion)
                {
                    isAxisRegion = true;
                    break;
                }
            }
        }

        if (!isAxisRegion)
        {
            var selecting = _sheet.Selection.SelectingRegion;
            if (selecting?.Spans(index, Axis) == true)
            {
                isSelected = true;
                if (Axis == Axis.Col && selecting is ColumnRegion ||
                    Axis == Axis.Row && selecting is RowRegion)
                {
                    isAxisRegion = true;
                }
            }
        }

        if (isAxisRegion)
            return "bds-selected-header-full";
        if (isSelected)
            return "bds-selected-header";
        return string.Empty;
    }
}
