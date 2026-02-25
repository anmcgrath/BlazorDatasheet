using System.Text;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render.Layout;
using BlazorDatasheet.Virtualise;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Render.Headings;

public partial class HeadingRenderer : SheetComponentBase
{
    [Parameter, EditorRequired] public Sheet? Sheet { get; set; }
    [Parameter] public Region? ViewRegion { get; set; }
    [Parameter] public bool AlternateAxisHeadingsShown { get; set; }
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

    private void SelectingChanged(object? sender, IRegion? e) => StateHasChanged();

    private void SelectionChanged(object? sender, SelectionChangedEventArgs e) => StateHasChanged();

    private void HandleRowColInserted(object? sender, RowColInsertedEventArgs? e)
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        StateHasChanged();
    }

    private void HandleRowColRemoved(object? sender, RowColRemovedEventArgs? e)
    {
        _viewRegion = ViewRegion ?? _sheet.Region;
        StateHasChanged();
    }

    private void HandleSizeModified(object? sender, SizeModifiedEventArgs e)
    {
        RefreshView();
    }

    public async Task RefreshView()
    {
        if (MainView is not null)
        {
            await MainView.RefreshView();
        }
    }

    protected string GetSelectedClass(int index)
    {
        bool isAxisRegion = false;
        bool isSelected = false;
        var regions = _sheet.Selection.Regions.Concat([_sheet.Selection.SelectingRegion]);
        foreach (var selection in regions)
        {
            if (selection?.Spans(index, Axis) == true)
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

        var sb = new StringBuilder();
        if (isAxisRegion)
            sb.Append("bds-selected-header-full");
        else if (isSelected)
            sb.Append("bds-selected-header");
        return sb.ToString();
    }
}