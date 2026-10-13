using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using BlazorDatasheet.Virtualise;
using Bunit;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class SpillRenderingTests
{
    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.JSInterop.SetupModule(x => x.Identifier == "createWindowEventsService");
        context.JSInterop.SetupModule(x => x.Identifier == "createHighlighter");
        context.Services.AddBlazorDatasheet();
        return context;
    }

    [Test]
    public void Spilled_Cells_Get_The_Spill_Class_And_The_Anchor_Does_Not()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetFormula(0, 0, "={1;2}");
        new VisualCell(0, 0, sheet, 13).ClassString.Should().NotContain("bds-spill-cell");
        new VisualCell(1, 0, sheet, 13).ClassString.Should().Contain("bds-spill-cell");
        new VisualCell(2, 0, sheet, 13).ClassString.Should().NotContain("bds-spill-cell");
    }

    [Test]
    public async Task Spill_Is_Outlined_While_The_Active_Cell_Is_Inside_It()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetFormula(0, 0, "={1;2;3}");
        var datasheet = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));

        bool HasOutline() => datasheet.FindAll("div").Any(d =>
            d.GetAttribute("style")?.Contains("--spill-outline-color") == true);

        await datasheet.InvokeAsync(() => sheet.Selection.Set(3, 0));
        HasOutline().Should().BeFalse();

        await datasheet.InvokeAsync(() => sheet.Selection.Set(1, 0));
        HasOutline().Should().BeTrue();

        await datasheet.InvokeAsync(() => sheet.Cells.ClearCells(new Region(0, 0)));
        HasOutline().Should().BeFalse();
    }

    [Test]
    public async Task Show_Formula_Shows_The_Anchor_Formula_In_Spilled_Cells()
    {
        await using var context = CreateContext();
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetFormula(0, 0, "={1;2}");
        var datasheet = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet).Add(x => x.ShowFormula, true));
        foreach (var virtualiser in datasheet.FindComponents<Virtualise2D>())
            await virtualiser.InvokeAsync(() => virtualiser.Instance.HandleScroll(new Rect(0, 0, 500, 500)));

        datasheet.Find(".bds-sheet-cell[data-row='1'][data-col='0']").TextContent.Should().Contain("={1;2}");
        datasheet.Find(".bds-sheet-cell[data-row='2'][data-col='0']").TextContent.Should().NotContain("=");
    }
}
