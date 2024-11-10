using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Edit;
using BlazorDatasheet.Events;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet;

public partial class DatasheetCssGrid : SheetComponentBase
{
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
    public Region? ViewRegion { get; set; } = null;

    private Region _viewRegion = new(0, 0);

    /// <summary>
    /// Datasheet theme that controls the css variables used to style the sheet.
    /// </summary>
    [Parameter]
    public string Theme { get; set; } = "default";

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

    /// <summary>
    /// Whether to show the column headings.
    /// </summary>
    [Parameter]
    public bool ShowColHeadings { get; set; } = true;

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

    [Parameter] public bool StickyHeaders { get; set; } = true;

    /// <summary>
    /// The size of the main region of this datasheet, that is the region of the grid without
    /// any frozen rows or columns.
    /// </summary>
    private Region MainViewRegion => new(
        Math.Max(FrozenTopCount, _viewRegion.Top),
        Math.Min(_viewRegion.Bottom - _frozenBottomCount, _viewRegion.Bottom),
        Math.Max(FrozenLeftCount, _viewRegion.Left),
        Math.Min(_viewRegion.Right - _frozenRightCount, _viewRegion.Right));

    protected override Task OnParametersSetAsync()
    {
        if (Sheet != _sheet)
        {
            RemoveEvents(_sheet);
            _sheet = Sheet ?? new(0, 0);
            AddEvents(_sheet);
        }

        if (ViewRegion != _viewRegion)
        {
            _viewRegion = ViewRegion ?? _sheet.Region;
        }

        if (_frozenLeftCount != FrozenLeftCount || _frozenRightCount != FrozenRightCount)
        {
            _frozenLeftCount = FrozenLeftCount;
            _frozenRightCount = FrozenRightCount;
            _frozenBottomCount = FrozenBottomCount;
            _frozenTopCount = FrozenTopCount;
        }

        StateHasChanged();

        return base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        Console.WriteLine($"Rendered {GetType().Name}");
        base.OnAfterRender(firstRender);
    }

    private void RemoveEvents(Sheet sheet)
    {
    }

    private void AddEvents(Sheet sheet)
    {
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
            { "Sheet", _sheet }
        };
    }

    private RenderFragment GetIconRenderFragment(string? cellIcon)
    {
        if (cellIcon != null && Icons.TryGetValue(cellIcon, out var rf))
            return rf;
        return _ => { };
    }
}