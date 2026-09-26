using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render.Layers;
using BlazorDatasheet.Virtualise;
using Bunit;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// The merges layer only does work when there are merges to show.
/// </summary>
public class VirtualMergesLayerTests
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

    private static IRenderedComponent<Datasheet> RenderSheet(BunitContext context, Sheet sheet)
    {
        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        foreach (var virtualiser in cut.FindComponents<Virtualise2D>())
        {
            Task? scroll = null;
            virtualiser.InvokeAsync(() => { scroll = virtualiser.Instance.HandleScroll(new Rect(0, 0, 500, 500)); })
                .Wait();
            scroll?.Wait();
        }

        return cut;
    }

    private static int RenderCount(IRenderedComponent<Datasheet> cut) =>
        cut.FindComponents<VirtualMergesLayer>().Sum(x => x.RenderCount);

    [Test]
    public async Task Setting_A_Value_With_No_Merges_Does_Not_Render_The_Merges_Layer()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = RenderSheet(context, sheet);

        var renders = RenderCount(cut);
        await cut.InvokeAsync(() => sheet.Cells.SetValue(0, 0, "hello"));
        // the pane still hands the layer its parameters again (twice per value change), but the layer doesn't ask
        // for a render itself
        RenderCount(cut).Should().Be(renders + 2);
    }

    [Test]
    public async Task Merging_And_Unmerging_Renders_The_Merges_Layer()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = RenderSheet(context, sheet);

        await cut.InvokeAsync(() => sheet.Cells.Merge(new Region(0, 1, 0, 1)));
        cut.Markup.Should().Contain("merged-cell");

        await cut.InvokeAsync(() => sheet.Cells.UnMerge([new Region(0, 1, 0, 1)]));
        cut.Markup.Should().NotContain("class=\"merged-cell\"");
    }

    [Test]
    public async Task Setting_A_Value_When_A_Merge_Is_In_View_Renders_The_Merges_Layer()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = RenderSheet(context, sheet);
        await cut.InvokeAsync(() => sheet.Cells.Merge(new Region(0, 1, 0, 1)));

        var renders = RenderCount(cut);
        await cut.InvokeAsync(() => sheet.Cells.SetValue(0, 0, "hello"));
        RenderCount(cut).Should().BeGreaterThan(renders);
        cut.Markup.Should().Contain("hello");
    }
}
