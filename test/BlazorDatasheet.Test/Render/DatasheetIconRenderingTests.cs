using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.SharedPages.Components.Examples.Formatting;
using BlazorDatasheet.Virtualise;
using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;
using BunitTestContext = Bunit.TestContext;

namespace BlazorDatasheet.Test.Render;

public class DatasheetIconRenderingTests
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task Metadata_Changes_Refresh_Conditional_Icons_Without_A_Manual_Render(bool bulk)
    {
        using var context = CreateContext();
        var sheet = new Sheet(1, 2);
        sheet.ConditionalFormats.Apply(sheet.Region, new ConditionalFormat(
            (position, currentSheet) => Equals(currentSheet.Cells.GetMetaData(position.row, position.col, "status"), "ready"),
            _ => new CellFormat { Icon = "tick" }));
        var sheetComponent = RenderSheet(context, sheet);
        sheetComponent.FindAll("[data-test-icon]").Should().BeEmpty();

        await sheetComponent.InvokeAsync(() =>
        {
            if (bulk)
                sheet.Cells.SetCellMetaData(sheet.Region, "status", "ready");
            else
                sheet.Cells.SetCellMetaData(0, 0, "status", "ready");
        });

        var expectedIcons = bulk ? 2 : 1;
        sheetComponent.FindAll("[data-test-icon]").Should().HaveCount(expectedIcons);
        await sheetComponent.InvokeAsync(() => sheet.Commands.Undo());
        sheetComponent.FindAll("[data-test-icon]").Should().BeEmpty();
        await sheetComponent.InvokeAsync(() => sheet.Commands.Redo());
        sheetComponent.FindAll("[data-test-icon]").Should().HaveCount(expectedIcons);

        await sheetComponent.InvokeAsync(() => sheet.Cells.ClearCellMetaData(0, 0));
        sheetComponent.FindAll("[data-test-icon]").Should().HaveCount(expectedIcons - 1);
        await sheetComponent.InvokeAsync(() => sheet.Commands.Undo());
        sheetComponent.FindAll("[data-test-icon]").Should().HaveCount(expectedIcons);
    }

    [Test]
    public async Task Suspended_Metadata_Changes_Render_Together_When_Updating_Resumes()
    {
        using var context = CreateContext();
        var sheet = new Sheet(3, 3);
        ApplyMetadataFormat(sheet);
        var component = RenderSheet(context, sheet);
        await component.InvokeAsync(() =>
        {
            sheet.ScreenUpdating = false;
            sheet.Cells.SetCellMetaData(0, 0, "status", "ready");
            sheet.Commands.BeginCommandGroup();
            sheet.Cells.SetCellMetaData(1, 1, "status", "ready");
            sheet.Commands.EndCommandGroup();
        });
        sheet.Cells.GetMetaData(0, 0, "status").Should().Be("ready");
        sheet.Cells.GetMetaData(1, 1, "status").Should().Be("ready");
        component.FindAll("[data-test-icon]").Should().BeEmpty();
        await component.InvokeAsync(() => sheet.ScreenUpdating = true);
        AssertMetadataAppearance(component, sheet, 0, 0, true);
        AssertMetadataAppearance(component, sheet, 1, 1, true);
    }

    [Test]
    public async Task Grouped_Metadata_Edit_Undo_Redo_Refreshes_Rendered_Cells()
    {
        using var context = CreateContext();
        var sheet = new Sheet(3, 3);
        ApplyMetadataFormat(sheet);
        var component = RenderSheet(context, sheet);
        await component.InvokeAsync(() =>
        {
            sheet.Commands.BeginCommandGroup();
            sheet.Cells.SetCellMetaData(1, 1, "status", "ready");
            sheet.Commands.EndCommandGroup();
        });
        AssertMetadataAppearance(component, sheet, 1, 1, true);
        await component.InvokeAsync(() => sheet.Commands.Undo());
        AssertMetadataAppearance(component, sheet, 1, 1, false);
        await component.InvokeAsync(() => sheet.Commands.Redo());
        AssertMetadataAppearance(component, sheet, 1, 1, true);
    }

    [TestCase(Axis.Row, false)]
    [TestCase(Axis.Col, false)]
    [TestCase(Axis.Row, true)]
    [TestCase(Axis.Col, true)]
    public async Task Structural_Edit_Undo_Redo_Refreshes_Metadata_Appearance(Axis axis, bool remove)
    {
        using var context = CreateContext();
        var sheet = new Sheet(4, 4);
        ApplyMetadataFormat(sheet);
        sheet.Cells.SetCellMetaData(1, 1, "status", "ready");
        sheet.Cells.SetValue(1, 1, "marker");
        var component = RenderSheet(context, sheet);
        var row = axis == Axis.Row ? (remove ? 0 : 2) : 1;
        var col = axis == Axis.Col ? (remove ? 0 : 2) : 1;
        await component.InvokeAsync(() =>
        {
            if (remove) sheet.GetRowColStore(axis).RemoveAt(0);
            else sheet.GetRowColStore(axis).InsertAt(0);
        });
        AssertMetadataAppearance(component, sheet, row, col, true);
        AssertMetadataAppearance(component, sheet, 1, 1, false);
        sheet.Cells[row, col].Value.Should().Be("marker");
        await component.InvokeAsync(() => sheet.Commands.Undo());
        AssertMetadataAppearance(component, sheet, 1, 1, true);
        AssertMetadataAppearance(component, sheet, row, col, false);
        sheet.Cells[1, 1].Value.Should().Be("marker");
        await component.InvokeAsync(() => sheet.Commands.Redo());
        AssertMetadataAppearance(component, sheet, row, col, true);
        AssertMetadataAppearance(component, sheet, 1, 1, false);
    }

    [TestCase(Axis.Row, 0)]
    [TestCase(Axis.Row, 2)]
    [TestCase(Axis.Col, 0)]
    [TestCase(Axis.Col, 2)]
    public async Task AllRegion_Covers_Every_Cell_After_Structural_Edit_And_History(Axis axis, int index)
    {
        using var context = CreateContext();
        var sheet = new Sheet(4, 4);
        sheet.ConditionalFormats.Apply(new AllRegion(), new ConditionalFormat((_, _) => true,
            _ => new CellFormat { Icon = "tick" }));
        var component = RenderSheet(context, sheet);
        void AssertCoverage()
        {
            for (var r = 0; r < sheet.NumRows; r++)
            for (var c = 0; c < sheet.NumCols; c++)
                (sheet.ConditionalFormats.GetFormatResult(r, c)?.Icon).Should().Be("tick");
            for (var row = 0; row < sheet.NumRows; row++)
            for (var col = 0; col < sheet.NumCols; col++)
            {
                (sheet.ConditionalFormats.GetFormatResult(row, col)?.Icon).Should().Be("tick");
                component.FindAll($"[data-row='{row}'][data-col='{col}'] [data-test-icon]")
                    .Should().ContainSingle($"cell ({row}, {col}) must show its conditional icon");
            }
            var region = sheet.ConditionalFormats.GetAllFormats().Should().ContainSingle().Subject.Region;
            region.Should().BeOfType<AllRegion>();
            region.Top.Should().Be(0);
            region.Left.Should().Be(0);
            region.Bottom.Should().Be(int.MaxValue);
            region.Right.Should().Be(int.MaxValue);
        }
        AssertCoverage();
        await component.InvokeAsync(() => sheet.GetRowColStore(axis).InsertAt(index));
        AssertCoverage();
        await component.InvokeAsync(() => sheet.Commands.Undo());
        AssertCoverage();
        await component.InvokeAsync(() => sheet.Commands.Redo());
        AssertCoverage();
        await component.InvokeAsync(() => sheet.GetRowColStore(axis).RemoveAt(index));
        AssertCoverage();
        await component.InvokeAsync(() => sheet.Commands.Undo());
        AssertCoverage();
        await component.InvokeAsync(() => sheet.Commands.Redo());
        AssertCoverage();
    }

    private static void ApplyMetadataFormat(Sheet sheet) =>
        sheet.ConditionalFormats.Apply(new Region(0, 20, 0, 20), new ConditionalFormat(
            (position, currentSheet) => Equals(currentSheet.Cells.GetMetaData(position.row, position.col, "status"), "ready"),
            _ => new CellFormat { Icon = "tick", IconColor = "green" }));

    private static void AssertMetadataAppearance(IRenderedComponent<Datasheet> component, Sheet sheet,
        int row, int col, bool ready)
    {
        sheet.Cells.GetMetaData(row, col, "status").Should().Be(ready ? "ready" : null);
        var icons = component.FindAll($"[data-row='{row}'][data-col='{col}'] [data-test-icon]");
        icons.Should().HaveCount(ready ? 1 : 0);
        if (ready) icons[0].ParentElement!.GetAttribute("style").Should().Contain("color: green");
    }

    [Test]
    public void Registered_Icon_Is_Rendered_In_The_Cell_With_Its_Icon_Color()
    {
        using var context = CreateContext();
        var sheet = new Sheet(1, 1);
        sheet.SetFormat(new Region(0, 0), new CellFormat { Icon = "tick", IconColor = "green" });

        var sheetComponent = RenderSheet(context, sheet);

        sheetComponent.FindAll("[data-test-icon]").Should().ContainSingle();
        sheetComponent.Find("[data-test-icon]").ParentElement!
            .GetAttribute("style").Should().Contain("color: green");
    }

    [Test]
    public void Icon_Falls_Back_To_The_Theme_Color_When_No_Icon_Color_Is_Set()
    {
        using var context = CreateContext();
        var sheet = new Sheet(1, 1);
        sheet.SetFormat(new Region(0, 0), new CellFormat { Icon = "tick" });

        var sheetComponent = RenderSheet(context, sheet);

        sheetComponent.Find("[data-test-icon]").ParentElement!
            .GetAttribute("style").Should().Contain("color: var(--icon-color)");
    }

    [Test]
    public void Unregistered_Icon_Name_Renders_No_Icon_Element_At_All()
    {
        using var context = CreateContext();
        var sheet = new Sheet(1, 1);
        sheet.SetFormat(new Region(0, 0), new CellFormat { Icon = "not-registered" });

        var sheetComponent = RenderSheet(context, sheet);

        sheetComponent.FindAll("[data-test-icon]").Should().BeEmpty();
        // the cell holds its content container and nothing else - no empty icon wrapper
        // taking up space to the left of the text.
        sheetComponent.FindAll("[data-row='0'][data-col='0'] > div").Should().ContainSingle()
            .Which.ClassList.Should().Contain("bds-cell-container");
    }

    [Test]
    public void Documentation_Example_Renders_Static_And_Conditional_Icons()
    {
        using var context = CreateContext();

        var example = context.RenderComponent<CellIconExample>();
        ShowViewport(example);

        // column A carries a flag on every row, straight from the cell format
        var flags = example.FindAll("[data-col='0']:not([data-row='-1']) > div:first-child");
        flags.Should().HaveCount(4);
        flags.Should().OnlyContain(f => f.GetAttribute("style")!.Contains("color: #7c3aed"));

        // column B picks its icon from the sign of the value, via a conditional format
        var signIcons = example.FindAll("[data-col='1']:not([data-row='-1']) > div:first-child");
        signIcons.Should().HaveCount(4);
        signIcons.Select(i => i.GetAttribute("style")!.Contains("color: #16a34a"))
            .Should().Equal(true, false, true, false);
    }

    private static BunitTestContext CreateContext()
    {
        var context = new BunitTestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var virtualiser = context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser");
        virtualiser.Setup<Rect>(x => x.Identifier == "calculateViewRect")
            .SetResult(new Rect(0, 0, 500, 500));
        context.Services.AddBlazorDatasheet();
        return context;
    }

    private static IRenderedComponent<Datasheet> RenderSheet(BunitTestContext context, Sheet sheet)
    {
        RenderFragment icon = builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "data-test-icon", "true");
            builder.CloseElement();
        };

        var component = context.RenderComponent<Datasheet>(parameters => parameters
            .Add(x => x.Sheet, sheet)
            .Add(x => x.Icons, new Dictionary<string, RenderFragment> { { "tick", icon } }));

        ShowViewport(component);
        return component;
    }
    
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
