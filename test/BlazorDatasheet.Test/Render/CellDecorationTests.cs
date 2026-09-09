using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using BlazorDatasheet.Virtualise;
using Bunit;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class CellDecorationTests
{
    [Test]
    public void Variables_Are_Snapshotted_Readonly_And_Compared_By_Value()
    {
        var input = new Dictionary<string, string> { ["--accent"] = "red" };
        var format = new CellFormat { CssVariables = input, BackgroundPattern = new(CellBackgroundPatternKind.Dots) };
        var clone = format.Clone();
        input["--accent"] = "blue";
        format.CssVariables!["--accent"].Should().Be("red");
        Action mutate = () => ((IDictionary<string, string>)format.CssVariables).Add("--other", "green");
        mutate.Should().Throw<NotSupportedException>();
        format.Equals(new CellFormat
        {
            CssVariables = new Dictionary<string, string> { ["--accent"] = "red" },
            BackgroundPattern = new(CellBackgroundPatternKind.Dots)
        }).Should().BeTrue();
        clone.CssVariables = input;
        clone.Equals(format).Should().BeFalse();
        format.CssVariables["--accent"].Should().Be("red");
    }

    [Test]
    public void Merge_Replaces_Whole_Properties_And_Explicit_Nulls_Clear()
    {
        var format = new CellFormat
        {
            CssVariables = new Dictionary<string, string> { ["--old"] = "red" },
            CssClass = "old", CornerFlagTopLeft = new("red"), CornerFlagTopRight = new("blue"),
            BackgroundPattern = new(CellBackgroundPatternKind.Dots)
        };
        var original = format.Clone();
        format.Merge(new CellFormat
        {
            CssVariables = new Dictionary<string, string> { ["--new"] = "blue" },
            CssClass = "new", CornerFlagTopLeft = null, BackgroundPattern = null
        });
        format.CssVariables!.Keys.Should().Equal("--new");
        format.CssClass.Should().Be("new");
        format.CornerFlagTopLeft.Should().BeNull();
        format.BackgroundPattern.Should().BeNull();
        format.CornerFlagTopRight.Should().Be(new CellCornerFlag("blue"));
        original.CornerFlagTopLeft.Should().NotBeNull();
        new CellFormat { CssClass = null }.Equals(new CellFormat { CssClass = null }).Should().BeTrue();
        new CellFormat { CssClass = null }.Equals(new CellFormat { BackgroundPattern = null }).Should().BeFalse();
        new CellFormat { CssClass = null }.Equals(new CellFormat()).Should().BeFalse();
    }

    [TestCase("color")]
    [TestCase("--bds-reserved")]
    [TestCase("--bad;name")]
    public void Invalid_Css_Variable_Names_Are_Rejected(string name)
    {
        Action assign = () => new CellFormat { CssVariables = new Dictionary<string, string> { [name] = "red" } };
        assign.Should().Throw<ArgumentException>();
    }

    [TestCase(CellBackgroundPatternKind.Horizontal, "0deg")]
    [TestCase(CellBackgroundPatternKind.Vertical, "90deg")]
    [TestCase(CellBackgroundPatternKind.Diagonal, "45deg")]
    [TestCase(CellBackgroundPatternKind.Crosshatch, "135deg")]
    [TestCase(CellBackgroundPatternKind.Dots, "radial-gradient")]
    public void Patterns_Render_Over_Background_With_Invariant_Sizes(CellBackgroundPatternKind kind, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var sheet = new Sheet(1, 1);
            sheet.Cells[0, 0].Format = new CellFormat
            {
                BackgroundColor = "red", BackgroundPattern = new(kind, "blue", 8.5, 1.5)
            };
            var style = new VisualCell(0, 0, sheet, 13).FormatStyleString;
            style.Should().Contain("background-color: red;").And.Contain(expected).And.Contain("8.5px").And.Contain("1.5px");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Test]
    public async Task Flags_And_Css_Render_And_Clear_With_Undo_Redo()
    {
        using var context = new Bunit.TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.Services.AddBlazorDatasheet();
        var sheet = new Sheet(2, 2);
        sheet.Range("A1:B1")!.Merge();
        sheet.Freeze(Edge.Top, 1);
        sheet.Cells[0, 0].Value = "Decorated";
        sheet.Cells[0, 0].Format = new CellFormat
        {
            CornerFlagTopLeft = new("red"), CornerFlagTopRight = new("blue", 12),
            CornerFlagBottomLeft = new("green"), CornerFlagBottomRight = new("yellow"),
            CssClass = "custom", CssVariables = new Dictionary<string, string> { ["--accent"] = "purple" }
        };
        var component = context.RenderComponent<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        foreach (var virtualiser in component.FindComponents<Virtualise2D>())
            await virtualiser.InvokeAsync(() => virtualiser.Instance.HandleScroll(new Rect(0, 0, 500, 500)));
        var cell = component.Find(".bds-sheet-cell[data-row='0'][data-col='0']");
        cell.ClassList.Should().Contain("custom").And.Contain("bds-sheet-cell");
        cell.GetAttribute("style").Should().Contain("--accent: purple;");
        cell.QuerySelectorAll(".bds-cell-corner-flag").Should().HaveCount(4);
        foreach (var corner in new[] { "top-left", "top-right", "bottom-left", "bottom-right" })
            cell.QuerySelector(".bds-cell-corner-" + corner)!.GetAttribute("aria-hidden").Should().Be("true");
        cell.QuerySelector(".bds-cell-corner-top-right")!.GetAttribute("style").Should().Contain("12px");
        component.Find(".bds-sheet-cell[data-row='1'][data-col='0']").Children.Should().ContainSingle();
        await component.InvokeAsync(() => sheet.Range("A1")!.Format = new CellFormat
        {
            CornerFlagTopLeft = null, CornerFlagTopRight = null, CornerFlagBottomLeft = null,
            CornerFlagBottomRight = null, CssClass = null, CssVariables = null
        });
        component.FindAll(".bds-cell-corner-flag").Should().BeEmpty();
        await component.InvokeAsync(() => sheet.Commands.Undo());
        component.Find(".bds-sheet-cell[data-row='0'][data-col='0']").QuerySelectorAll(".bds-cell-corner-flag").Should().HaveCount(4);
        await component.InvokeAsync(() => sheet.Commands.Redo());
        component.FindAll(".bds-cell-corner-flag").Should().BeEmpty();
    }

    [Test]
    public void Conditional_Decorations_Override_And_Clear_Without_Changing_Base_Format()
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells[0, 0].Format = new CellFormat
        {
            BackgroundPattern = new(CellBackgroundPatternKind.Dots), CornerFlagTopLeft = new("red"), CssClass = "base"
        };
        sheet.ConditionalFormats.Apply(sheet.Region, new ConditionalFormat((_, _) => true,
            _ => new CellFormat { BackgroundPattern = null, CornerFlagTopLeft = new("blue"), CssClass = "conditional" }));
        var visual = new VisualCell(0, 0, sheet, 13);
        visual.FormatStyleString.Should().NotContain("background-image");
        visual.ClassString.Should().Contain("conditional").And.NotContain("base");
        visual.Format!.CornerFlagTopLeft!.Color.Should().Be("blue");
        sheet.Cells[0, 0].Format!.CornerFlagTopLeft!.Color.Should().Be("red");
    }
}
