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
using FluentAssertions;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The datasheet only re-renders when something it shows has changed.
/// </summary>
public class DatasheetRenderGatingTests
{
    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    [Test]
    public void Rendering_Host_Again_With_Unchanged_Parameters_Does_Not_Re_Render_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default")
            .Add(x => x.IsReadOnly, false));

        var renderCount = cut.RenderCount;
        // the same parameters the host supplied the first time
        cut.SetParametersAndRender(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default")
            .Add(x => x.IsReadOnly, false));
        cut.RenderCount.Should().Be(renderCount);
    }

    [Test]
    public void Stable_Render_Fragment_Re_Renders_When_Its_Captured_State_Changes()
    {
        using var context = CreateContext();
        var host = context.RenderComponent<StableTemplateHost>();
        ShowViewport(host);
        host.Markup.Should().Contain("before");

        host.InvokeAsync(host.Instance.ChangeHeading);

        host.Markup.Should().Contain("after");
    }

    [Test]
    public void Changing_Theme_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Theme, "default"));

        var renderCount = cut.RenderCount;
        cut.SetParametersAndRender(p => p.Add(x => x.Theme, "dark"));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Changing_IsReadOnly_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.IsReadOnly, false));

        var renderCount = cut.RenderCount;
        cut.SetParametersAndRender(p => p.Add(x => x.IsReadOnly, true));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Changing_Sheet_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.SetParametersAndRender(p => p.Add(x => x.Sheet, new Sheet(5, 5)));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Adding_A_Column_Group_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.InvokeAsync(() => sheet.Columns.SetGroup(1, 3, "Q1"));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Hiding_A_Row_Re_Renders_Datasheet()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        var renderCount = cut.RenderCount;
        cut.InvokeAsync(() => sheet.Rows.Hide(2, 3));
        cut.RenderCount.Should().BeGreaterThan(renderCount);
    }

    [Test]
    public void Heading_Regions_Are_Recalculated_When_Rows_Are_Inserted()
    {
        using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        var headings = cut.FindAll("div.bds-row-head[data-row]").Count;

        cut.InvokeAsync(() => sheet.Rows.InsertAt(0, 2));
        ShowViewport(cut);

        cut.FindAll("div.bds-row-head[data-row]").Count.Should().Be(headings + 2);
    }

    [Test]
    public void Heading_Regions_Are_Recalculated_When_The_Freeze_State_Changes()
    {
        using var context = CreateContext();
        var sheet = new Sheet(20, 20);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
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
    public void Re_Supplying_Equal_Custom_Cell_Types_Does_Not_Re_Render_Grid_Rows()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var definition = CellTypeDefinition.Create<TextEditorComponent, TextRenderer>();
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", definition } }));
        ShowViewport(cut);

        var rowRenders = GridRowRenderCount(cut);

        // a different dictionary instance holding the same definitions
        cut.SetParametersAndRender(p => p
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", definition } }));

        GridRowRenderCount(cut).Should().Be(rowRenders);
    }

    [Test]
    public void Changing_A_Custom_Cell_Type_Does_Re_Render_Grid_Rows()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", CellTypeDefinition.Create<TextEditorComponent, TextRenderer>() } }));
        ShowViewport(cut);

        var rowRenders = GridRowRenderCount(cut);

        cut.SetParametersAndRender(p => p
            .Add(x => x.CustomCellTypeDefinitions, new Dictionary<string, CellTypeDefinition>
                { { "custom", CellTypeDefinition.Create<TextEditorComponent, BoolRenderer>() } }));

        GridRowRenderCount(cut).Should().BeGreaterThan(rowRenders);
    }

    /// <summary>
    /// A parameter that only the pane's layers read reaches them without the cells being rebuilt.
    /// </summary>
    [Test]
    public void Changing_UseAutoFill_Re_Renders_The_Datasheet_But_Not_The_Grid_Rows()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.UseAutoFill, true));
        ShowViewport(cut);

        var renderCount = cut.RenderCount;
        var rowRenders = GridRowRenderCount(cut);

        cut.SetParametersAndRender(p => p.Add(x => x.UseAutoFill, false));

        cut.RenderCount.Should().BeGreaterThan(renderCount);
        GridRowRenderCount(cut).Should().Be(rowRenders);
    }

    /// <summary>
    /// The mouse up at the end of a plain click reports that nothing is being selected any more.
    /// The headings already show that selection, so it costs them nothing.
    /// </summary>
    [Test]
    public void Repeating_A_Selection_Does_Not_Re_Render_The_Row_Headings()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        var headings = cut.FindComponent<RowHeadingRenderer>();
        var renderCount = headings.RenderCount;

        cut.InvokeAsync(() => sheet.Selection.Set(2, 2));
        headings.RenderCount.Should().Be(renderCount + 1);

        // the selecting-changed that follows the click, with the selection unchanged
        cut.InvokeAsync(() => sheet.Selection.CancelSelecting());
        headings.RenderCount.Should().Be(renderCount + 1);
    }

    [Test]
    public void Selecting_A_Different_Region_Does_Re_Render_The_Row_Headings()
    {
        using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        ShowViewport(cut);

        var headings = cut.FindComponent<RowHeadingRenderer>();
        cut.InvokeAsync(() => sheet.Selection.Set(2, 2));
        var renderCount = headings.RenderCount;

        cut.InvokeAsync(() => sheet.Selection.Set(4, 2));

        headings.RenderCount.Should().Be(renderCount + 1);
    }

    private static int GridRowRenderCount(IRenderedFragment component) =>
        component.FindComponents<DatasheetGridRow>().Sum(x => x.RenderCount);

    private static void ShowViewport(IRenderedFragment component)
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
