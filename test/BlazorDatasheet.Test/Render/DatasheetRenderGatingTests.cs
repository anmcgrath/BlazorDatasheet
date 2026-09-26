using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Render.Headings;
using BlazorDatasheet.Virtualise;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The datasheet only re-renders when something it shows has changed.
/// </summary>
public class DatasheetRenderGatingTests
{
    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    [Test]
    public async Task Rendering_Host_Again_With_Unchanged_Parameters_Does_Not_Re_Render_Datasheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default")
            .Add(x => x.IsReadOnly, false));

        var renderCount = cut.RenderCount;
        // the same parameters the host supplied the first time
        cut.Render(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default")
            .Add(x => x.IsReadOnly, false));
        cut.RenderCount.Should().Be(renderCount);
    }

    [Test]
    public async Task Stable_Render_Fragment_Re_Renders_When_Its_Captured_State_Changes()
    {
        await using var context = CreateContext();
        var host = context.Render<StableTemplateHost>();
        ShowViewport(host);
        host.Markup.Should().Contain("before");

        host.InvokeAsync(host.Instance.ChangeHeading);

        host.Markup.Should().Contain("after");
    }

    [Test]
    public async Task Changing_Theme_Re_Renders_Datasheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default"));

        var renderCount = cut.RenderCount;
        cut.Render(p => p.Add(x => x.Theme, "dark"));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public async Task Changing_IsReadOnly_Re_Renders_Datasheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.IsReadOnly, false));

        var renderCount = cut.RenderCount;
        cut.Render(p => p.Add(x => x.IsReadOnly, true));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public async Task Changing_Sheet_Re_Renders_Datasheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.Render(p => p.Add(x => x.Sheet, new Sheet(5, 5)));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public async Task Adding_A_Column_Group_Re_Renders_Datasheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.InvokeAsync(() => sheet.Columns.SetGroup(1, 3, "Q1"));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public async Task Hiding_A_Row_Re_Renders_Datasheet()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.InvokeAsync(() => sheet.Rows.Hide(2, 3));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public async Task Heading_Regions_Are_Recalculated_When_Rows_Are_Inserted()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        var headings = cut.FindAll("div.bds-row-head[data-row]").Count;

        cut.InvokeAsync(() => sheet.Rows.InsertAt(0, 2));
        ShowViewport(cut);

        cut.FindAll("div.bds-row-head[data-row]").Count.Should().Be(headings + 2);
    }

    [Test]
    public async Task Heading_Regions_Are_Recalculated_When_The_Freeze_State_Changes()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(20, 20);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        cut.InvokeAsync(() => sheet.FreezeLeftColumns(2));
        ShowViewport(cut);

        cut.FindAll("div.bds-frozen-left").Should().NotBeEmpty();
    }

    /// <summary>
    /// A host that builds its cell type dictionary in markup hands over a new instance on every one
    /// of its renders. That must not cost a rebuild of every visible cell.
    /// </summary>
    [Test]
    public async Task Re_Supplying_Equal_Custom_Cell_Types_Does_Not_Re_Render_Grid_Rows()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var definition = CellTypeDefinition.Create<TextEditorComponent, TextRenderer>();
        var cut = context.Render<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", definition } }));
        ShowViewport(cut);

        var rowRenders = GridRowRenderCount(cut);

        // a different dictionary instance holding the same definitions
        cut.Render(p => p
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", definition } }));

        GridRowRenderCount(cut).Should().Be(rowRenders);
    }

    [Test]
    public async Task Changing_A_Custom_Cell_Type_Does_Re_Render_Grid_Rows()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", CellTypeDefinition.Create<TextEditorComponent, TextRenderer>() } }));
        ShowViewport(cut);

        var rowRenders = GridRowRenderCount(cut);

        cut.Render(p => p
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", CellTypeDefinition.Create<TextEditorComponent, BoolRenderer>() } }));

        GridRowRenderCount(cut).Should().BeGreaterThan(rowRenders);
    }

    /// <summary>
    /// A parameter that only the pane's layers read reaches them without the cells being rebuilt.
    /// </summary>
    [Test]
    public async Task Changing_UseAutoFill_Re_Renders_The_Datasheet_But_Not_The_Grid_Rows()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.UseAutoFill, true));
        ShowViewport(cut);

        var renderCount = cut.RenderCount;
        var rowRenders = GridRowRenderCount(cut);

        cut.Render(p => p.Add(x => x.UseAutoFill, false));

        cut.RenderCount.Should().BeGreaterThan(renderCount);
        GridRowRenderCount(cut).Should().Be(rowRenders);
    }

    /// <summary>
    /// The mouse up at the end of a plain click reports that nothing is being selected any more.
    /// The headings already show that selection, so it costs them nothing.
    /// </summary>
    [Test]
    public async Task Repeating_A_Selection_Does_Not_Re_Render_The_Row_Headings()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        var headings = cut.FindComponent<RowHeadingRenderer>();
        var renderCount = headings.RenderCount;

        cut.InvokeAsync(() => sheet.Selection.Set(2, 2));
        headings.RenderCount.Should().BeGreaterThan(renderCount);
        renderCount = headings.RenderCount;

        // the selecting-changed that follows the click, with the selection unchanged
        cut.InvokeAsync(() => sheet.Selection.CancelSelecting());
        headings.RenderCount.Should().Be(renderCount);
    }

    [Test]
    public async Task Selecting_A_Different_Region_Does_Re_Render_The_Row_Headings()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        var headings = cut.FindComponent<RowHeadingRenderer>();
        cut.InvokeAsync(() => sheet.Selection.Set(2, 2));
        var renderCount = headings.RenderCount;

        cut.InvokeAsync(() => sheet.Selection.Set(4, 2));

        headings.RenderCount.Should().BeGreaterThan(renderCount);
    }

    private static int GridRowRenderCount(IRenderedComponent<IComponent> component) =>
        component.FindComponents<DatasheetGridRow>().Sum(x => x.RenderCount);

    private static void ShowViewport(IRenderedComponent<IComponent> component)
    {
        foreach (var virtualiser in component.FindComponents<Virtualise2D>())
        {
            Task? scroll = null;
            virtualiser.InvokeAsync(() => { scroll = virtualiser.Instance.HandleScroll(new Rect(0, 0, 500, 500)); })
                .Wait();
            scroll?.Wait();
        }
    }
}
