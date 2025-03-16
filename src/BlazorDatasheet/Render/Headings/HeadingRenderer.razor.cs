using System.Text;
using BlazorDatasheet.Core.Data;
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

    protected Virtualise2D MainView = default!;

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
            _sheet = Sheet ?? new(0, 0);

            _sheet.Selection.SelectionChanged += (_, _) => StateHasChanged();
            _sheet.Selection.SelectingChanged += (_, _) => StateHasChanged();
            _sheet.Rows.SizeModified += HandleSizeModified;
            _sheet.Rows.Inserted += HandleRowColInserted;
            _sheet.Columns.Inserted += HandleRowColInserted;
            _sheet.Rows.Removed += HandleRowColRemoved;
            _sheet.Columns.Removed += HandleRowColRemoved;
            _sheet.Columns.SizeModified += HandleSizeModified;

            LayoutProvider = Axis == Axis.Col
                ? new ColHeadingLayoutProvider(_sheet)
                : new RowHeadingLayoutProvider(_sheet);

            refreshView = true;
        }

        if (ViewRegion != _viewRegion)
        {
            _viewRegion = ViewRegion ?? _sheet.Region;
            refreshView = true;
        }

        if (refreshView)
            await RefreshView();
    }

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