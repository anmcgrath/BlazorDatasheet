using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Extensions;
using BlazorDatasheet.Render;
using BlazorDatasheet.Virtualise;
using System.Linq;
using System.Threading.Tasks;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

/// <summary>
/// An edge shared by two cells is drawn once, by the cell before it, whichever cell holds the border.
/// </summary>
public class VisualCellBorderTests
{
    private static Border Border(string color, int width = 1) => new() { Color = color, Width = width };

    private static VisualCell Cell(Sheet sheet, int row, int col) => new(row, col, sheet, 12);

    [Test]
    public void First_Row_And_Column_Draw_Their_Top_And_Left_Borders()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new Region(0, 0), new CellFormat { BorderLeft = Border("red"), BorderTop = Border("blue") });

        var style = Cell(sheet, 0, 0).FormatStyleString;
        style.Should().Contain("border-left: 1px solid red");
        style.Should().Contain("border-top: 1px solid blue");
        style.Should().NotContain(";;");
    }

    [Test]
    public void Left_And_Top_Borders_Are_Drawn_By_The_Cells_Before()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new Region(1, 1), new CellFormat { BorderLeft = Border("red"), BorderTop = Border("blue") });

        var own = Cell(sheet, 1, 1);
        own.Borders.Any.Should().BeFalse();
        own.FormatStyleString.Should().NotContain("border");

        Cell(sheet, 1, 0).FormatStyleString.Should().Be("border-right: 1px solid red;");
        Cell(sheet, 0, 1).FormatStyleString.Should().Be("border-bottom: 1px solid blue;");
    }

    [Test]
    public void Border_Set_Without_The_Command_Is_Drawn()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells[1, 1].Format = new CellFormat { BorderLeft = Border("red") };
        Cell(sheet, 1, 0).Borders.Right!.Color.Should().Be("red");
    }

    [Test]
    public void Border_Set_Last_Is_Drawn()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new Region(1, 1), new CellFormat { BorderLeft = Border("red") });
        sheet.SetFormat(new Region(1, 0), new CellFormat { BorderRight = Border("blue") });
        Cell(sheet, 1, 0).Borders.Right!.Color.Should().Be("blue");

        sheet.SetFormat(new Region(1, 1), new CellFormat { BorderLeft = Border("green") });
        Cell(sheet, 1, 0).Borders.Right!.Color.Should().Be("green");

        sheet.SetFormat(new Region(1, 1), new CellFormat { BorderLeft = null });
        Cell(sheet, 1, 0).Borders.Any.Should().BeFalse();
    }

    [Test]
    public void Row_And_Column_Borders_Are_Drawn_By_The_Cells_Before()
    {
        var sheet = new Sheet(4, 4);
        sheet.SetFormat(new ColumnRegion(2), new CellFormat { BorderLeft = Border("red") });
        sheet.SetFormat(new RowRegion(2), new CellFormat { BorderTop = Border("blue") });

        Cell(sheet, 0, 1).Borders.Right!.Color.Should().Be("red");
        Cell(sheet, 1, 0).Borders.Bottom!.Color.Should().Be("blue");
        Cell(sheet, 1, 1).Borders.Should().Be(new CellBorders(null, null, Cell(sheet, 1, 1).Borders.Right,
            Cell(sheet, 1, 1).Borders.Bottom));
        Cell(sheet, 1, 1).Borders.Right!.Color.Should().Be("red");
        Cell(sheet, 1, 1).Borders.Bottom!.Color.Should().Be("blue");
    }

    [Test]
    public void Conditional_Format_Left_And_Top_Borders_Are_Drawn_Over_Stored_Borders()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells[1, 0].Format = new CellFormat { BorderRight = Border("red") };
        sheet.Cells[0, 1].Format = new CellFormat { BorderBottom = Border("red") };
        sheet.ConditionalFormats.Apply(new Region(1, 1), new ConditionalFormat(
            (position, s) => s.Cells.GetValue(position.row, position.col) is double and > 0,
            _ => new CellFormat { BorderLeft = Border("blue", 2), BorderTop = Border("blue", 2) }));

        Cell(sheet, 1, 0).Borders.Right!.Color.Should().Be("red");
        Cell(sheet, 0, 1).Borders.Bottom!.Color.Should().Be("red");

        sheet.Cells.SetValue(1, 1, 5);
        Cell(sheet, 1, 0).FormatStyleString.Should().Be("border-right: 2px solid blue;");
        Cell(sheet, 0, 1).FormatStyleString.Should().Be("border-bottom: 2px solid blue;");
    }

    [Test]
    public void Conditional_Format_Border_Of_The_Cell_After_The_Edge_Decides()
    {
        var sheet = new Sheet(1, 2);
        sheet.ConditionalFormats.Apply(new Region(0, 0),
            new ConditionalFormat((_, _) => true, _ => new CellFormat { BorderRight = Border("red") }));
        sheet.ConditionalFormats.Apply(new Region(0, 1),
            new ConditionalFormat((_, _) => true, _ => new CellFormat { BorderLeft = Border("blue") }));

        Cell(sheet, 0, 0).Borders.Right!.Color.Should().Be("blue");
    }

    [Test]
    public void Hidden_Column_Does_Not_Hide_A_Border()
    {
        var sheet = new Sheet(3, 4);
        sheet.Cells[1, 2].Format = new CellFormat { BorderLeft = Border("red") };
        sheet.Cells[1, 0].Format = new CellFormat { BorderLeft = Border("blue") };
        sheet.Columns.Hide(1, 1);

        Cell(sheet, 1, 0).Borders.Right!.Color.Should().Be("red");
        Cell(sheet, 1, 0).Borders.Left!.Color.Should().Be("blue");

        sheet.Columns.Hide(0, 1);
        Cell(sheet, 1, 2).Borders.Left!.Color.Should().Be("red");
    }

    [Test]
    public void Merged_Cell_Shares_Edges_With_The_Cells_Around_It()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.Merge(new Region(1, 2, 1, 2));
        sheet.Cells[1, 1].Format = new CellFormat { BorderLeft = Border("red"), BorderTop = Border("red") };
        sheet.Cells[1, 3].Format = new CellFormat { BorderLeft = Border("blue") };
        sheet.Cells[3, 1].Format = new CellFormat { BorderTop = Border("green") };

        var merged = Cell(sheet, 1, 1);
        merged.Borders.Right!.Color.Should().Be("blue");
        merged.Borders.Bottom!.Color.Should().Be("green");
        merged.Borders.Left.Should().BeNull();

        Cell(sheet, 1, 0).Borders.Right!.Color.Should().Be("red");
        Cell(sheet, 2, 0).Borders.Right!.Color.Should().Be("red");
        Cell(sheet, 0, 2).Borders.Bottom!.Color.Should().Be("red");
    }

    [Test]
    public void Unbordered_Sheet_Has_No_Inline_Style()
    {
        var sheet = new Sheet(3, 3);
        Cell(sheet, 1, 1).FormatStyleString.Should().BeEmpty();
    }

    [Test]
    public async Task Changing_A_Cell_Redraws_The_Cells_That_Draw_Its_Edges()
    {
        await using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule(x => x.Identifier == "getVirtualiser")
            .Setup<Rect>(x => x.Identifier == "calculateViewRect").SetResult(new Rect(0, 0, 500, 500));
        context.Services.AddBlazorDatasheet();

        var sheet = new Sheet(5, 5);
        sheet.ConditionalFormats.Apply(new Region(3, 3), new ConditionalFormat(
            (position, s) => s.Cells.GetValue(position.row, position.col) is double and > 0,
            _ => new CellFormat { BorderTop = Border("blue") }));

        var cut = context.Render<Datasheet>(p => p.Add(x => x.Sheet, sheet));
        foreach (var virtualiser in cut.FindComponents<Virtualise2D>())
        {
            Task? scroll = null;
            await virtualiser.InvokeAsync(() =>
            {
                scroll = virtualiser.Instance.HandleScroll(new Rect(0, 0, 500, 500));
            });
            if (scroll != null)
                await scroll;
        }

        string? Style(int row, int col) =>
            cut.Find($".bds-sheet-cell[data-row=\"{row}\"][data-col=\"{col}\"]").GetAttribute("style");

        Style(1, 0).Should().BeNull();

        await cut.InvokeAsync(() => sheet.Cells[1, 1].Format = new CellFormat { BorderLeft = Border("red") });
        Style(1, 0).Should().Contain("border-right: 1px solid red");

        await cut.InvokeAsync(() => sheet.Cells.SetValue(3, 3, 5));
        Style(2, 3).Should().Contain("border-bottom: 1px solid blue");

        await cut.InvokeAsync(() => sheet.Columns.Hide(0, 1));
        Style(1, 1).Should().Contain("border-left: 1px solid red");
    }
}
