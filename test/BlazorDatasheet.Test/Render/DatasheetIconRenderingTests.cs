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
