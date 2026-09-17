using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using Bunit;
using FluentAssertions;
using NUnit.Framework;
using TestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class DatasheetCurrentSheetSelectionTests
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

    private static IRenderedComponent<Datasheet> Render(TestContext context, Sheet sheet, bool showAlways = false)
    {
        sheet.Selection.Set(1, 1);
        return context.RenderComponent<Datasheet>(p => p
            .Add(x => x.Sheet, sheet)
            .Add(x => x.ShowSelectionWhenNotCurrentSheet, showAlways));
    }

    private static bool ShowsSelection(IRenderedComponent<Datasheet> component) =>
        component.FindAll(".bds-selection-layer").Count > 0;

    [Test]
    public void Selections_Are_Shown_Before_Any_Sheet_Has_Been_Used()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var first = Render(context, workbook.AddSheet(10, 10));
        var second = Render(context, workbook.AddSheet(10, 10));

        ShowsSelection(first).Should().BeTrue();
        ShowsSelection(second).Should().BeTrue();
    }

    [Test]
    public async Task Only_The_Sheet_Last_Used_In_The_Workbook_Shows_Its_Selection()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet(10, 10);
        var first = Render(context, sheet1);
        var second = Render(context, workbook.AddSheet(10, 10));

        await first.InvokeAsync(() => first.Instance.SetActiveAsync());
        ShowsSelection(first).Should().BeTrue();
        ShowsSelection(second).Should().BeFalse();

        await first.InvokeAsync(() => first.Instance.SetActiveAsync(false));
        await second.InvokeAsync(() => second.Instance.SetActiveAsync());
        ShowsSelection(first).Should().BeFalse();
        ShowsSelection(second).Should().BeTrue();

        // hidden, not cleared
        sheet1.Selection.ActiveRegion.Should().NotBeNull();
    }

    [Test]
    public async Task Selection_Stays_When_Focus_Leaves_The_Workbook()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var first = Render(context, workbook.AddSheet(10, 10));
        var second = Render(context, workbook.AddSheet(10, 10));

        await first.InvokeAsync(() => first.Instance.SetActiveAsync());
        await first.InvokeAsync(() => first.Instance.SetActiveAsync(false));

        ShowsSelection(first).Should().BeTrue();
        ShowsSelection(second).Should().BeFalse();
    }

    [Test]
    public async Task Sheets_Of_Different_Workbooks_Do_Not_Affect_Each_Other()
    {
        using var context = CreateContext();
        var first = Render(context, new Sheet(10, 10));
        var second = Render(context, new Sheet(10, 10));

        await first.InvokeAsync(() => first.Instance.SetActiveAsync());

        ShowsSelection(first).Should().BeTrue();
        ShowsSelection(second).Should().BeTrue();
    }

    [Test]
    public async Task Selection_Can_Be_Kept_Visible()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var first = Render(context, workbook.AddSheet(10, 10));
        var second = Render(context, workbook.AddSheet(10, 10), showAlways: true);

        await first.InvokeAsync(() => first.Instance.SetActiveAsync());

        ShowsSelection(second).Should().BeTrue();
    }

    [Test]
    public async Task The_Sheet_With_The_Formula_Being_Edited_Keeps_Its_Selection_While_Another_Is_Used()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet(10, 10);
        var first = Render(context, sheet1);
        var second = Render(context, workbook.AddSheet(10, 10));

        await first.InvokeAsync(async () =>
        {
            await first.Instance.SetActiveAsync();
            sheet1.Editor.BeginEdit(1, 1, true, EditEntryMode.Key, "=");
        });
        await first.InvokeAsync(() => first.Instance.SetActiveAsync(false));
        await second.InvokeAsync(() => second.Instance.SetActiveAsync());

        ShowsSelection(first).Should().BeTrue();
        ShowsSelection(second).Should().BeFalse();

        await first.InvokeAsync(() => sheet1.Editor.CancelEdit());

        first.WaitForAssertion(() => ShowsSelection(first).Should().BeFalse());
        ShowsSelection(second).Should().BeTrue();
    }

    [Test]
    public async Task A_Datasheet_That_Is_Given_Another_Sheet_Shows_Its_Selection()
    {
        using var context = CreateContext();
        var workbook = new Workbook();
        var sheet2 = workbook.AddSheet(10, 10);
        sheet2.Selection.Set(2, 2);
        var component = Render(context, workbook.AddSheet(10, 10));

        await component.InvokeAsync(() => component.Instance.SetActiveAsync());
        await component.InvokeAsync(() => component.Instance.SetActiveAsync(false));
        component.SetParametersAndRender(p => p.Add(x => x.Sheet, sheet2));

        ShowsSelection(component).Should().BeTrue();
    }
}
