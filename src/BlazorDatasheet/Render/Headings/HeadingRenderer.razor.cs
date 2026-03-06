using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render.Layout;
using BlazorDatasheet.Virtualise;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Render.Headings;

public partial class HeadingRenderer : SheetComponentBase, IDisposable
{
    [Parameter, EditorRequired] public Sheet? Sheet { get; set; }
    [Parameter] public Region? ViewRegion { get; set; }
    [Parameter, EditorRequired] public RenderFragment<HeadingContext> ChildContent { get; set; } = null!;

    protected Virtualise2D? MainView;

    protected Region _viewRegion = new(-1, -1);
    protected Sheet _sheet = new(0, 0);
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
            if (Sheet is not null)
                UnSubscribeEvents(Sheet);

            _sheet = Sheet ?? new(0, 0);
            SubscribeEvents(_sheet);

            LayoutProvider = Axis == Axis.Col
                ? new ColHeadingLayoutProvider(_sheet)
                : new RowHeadingLayoutProvider(_sheet);
            LayoutProvider.ViewRegion = _viewRegion;

            refreshView = true;
        }

        if (ViewRegion != _viewRegion)
        {
            _viewRegion = ViewRegion ?? _sheet.Region;
            LayoutProvider.ViewRegion = _viewRegion;
            refreshView = true;
        }

        if (refreshView)
        {
            _dirty = true;
            await RefreshView();
        }
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
    }

    private bool _dirty;

    private void SelectingChanged(object? sender, IRegion? e)
    {
        if (e != null)
        {
            var spans = Axis == Axis.Col
                ? e.Right >= _viewRegion.Left && e.Left <= _viewRegion.Right
                : e.Bottom >= _viewRegion.Top && e.Top <= _viewRegion.Bottom;
            if (!spans)
                return;
        }

        _dirty = true;
        StateHasChanged();
    }

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        _dirty = true;
        StateHasChanged();
    }

    protected override bool ShouldRender()
    {
        if (!_dirty) return false;
        _dirty = false;
        return true;
    }

    private void HandleRowColInserted(object? sender, RowColInsertedEventArgs? e)
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        _dirty = true;
        StateHasChanged();
    }

    private void HandleRowColRemoved(object? sender, RowColRemovedEventArgs? e)
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        _dirty = true;
        StateHasChanged();
    }

    private void HandleSizeModified(object? sender, SizeModifiedEventArgs e)
    {
        _dirty = true;
        RefreshView();
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
