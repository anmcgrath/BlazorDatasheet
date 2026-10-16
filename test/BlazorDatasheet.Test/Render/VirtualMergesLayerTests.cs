using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
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

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public async Task Changing_A_Neighbours_Border_Redraws_The_Merge(bool below, bool hiddenGap)
    {
        await using var context = CreateContext();
        var sheet = new Sheet(6, 6);
        sheet.Cells.Merge(new Region(1, 2, 1, 2));
        if (hiddenGap)
        {
            if (below)
                sheet.Rows.Hide(3, 1);
            else
                sheet.Columns.Hide(3, 1);
        }
        var cut = RenderSheet(context, sheet);
        var neighbour = hiddenGap ? 4 : 3;
        var row = below ? neighbour : 1;
        var col = below ? 1 : neighbour;
        var border = new Border { Color = "red", Width = 2 };
        var format = below ? new CellFormat { BorderTop = border } : new CellFormat { BorderLeft = border };
        var expected = below ? "border-bottom: 2px solid red" : "border-right: 2px solid red";

        await cut.InvokeAsync(() => sheet.Cells[row, col].Format = format);

        cut.Find(".bds-merged-cell .bds-sheet-cell").GetAttribute("style").Should().Contain(expected);

        await cut.InvokeAsync(() => sheet.Cells[row, col].Format = below
            ? new CellFormat { BorderTop = null }
            : new CellFormat { BorderLeft = null });

        cut.Find(".bds-merged-cell .bds-sheet-cell").GetAttribute("style").Should().BeNullOrEmpty();
    }

    [Test]
    public async Task Setting_A_Value_With_No_Merges_Does_Not_Render_The_Merges_Layer()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = RenderSheet(context, sheet);

        var renders = RenderCount(cut);
        await cut.InvokeAsync(() => sheet.Cells.SetValue(0, 0, "hello"));
        // the layer container only renders its layers when something they take from it changes
        RenderCount(cut).Should().Be(renders);
    }

    [Test]
    public async Task Merging_And_Unmerging_Renders_The_Merges_Layer()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(10, 10);
        var cut = RenderSheet(context, sheet);

        await cut.InvokeAsync(() => sheet.Cells.Merge(new Region(0, 1, 0, 1)));
        cut.Markup.Should().Contain("bds-merged-cell");

        await cut.InvokeAsync(() => sheet.Cells.UnMerge([new Region(0, 1, 0, 1)]));
        cut.Markup.Should().NotContain("class=\"bds-merged-cell\"");
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
