using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Render;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetFormatCommandTests
{
    [TestCase("cells")]
    [TestCase("rows")]
    [TestCase("columns")]
    public void Setting_Borders_Clears_The_Neighbours_Side_Of_Shared_Edges_And_Undo_Redo_Restores_Formats(string kind)
    {
        var sheet = new Sheet(5, 5);
        sheet.SetFormat(sheet.Region, new CellFormat
        {
            BackgroundColor = "yellow",
            BorderLeft = new Border { Width = 2, Color = "red" },
            BorderTop = new Border { Width = 2, Color = "red" },
            BorderRight = new Border { Width = 2, Color = "red" },
            BorderBottom = new Border { Width = 2, Color = "red" }
        });
        IRegion region = kind switch
        {
            "rows" => new RowRegion(2, 3),
            "columns" => new ColumnRegion(2, 3),
            _ => new Region(2, 3, 2, 3)
        };
        sheet.SetFormat(region, new CellFormat
        {
            BorderLeft = new Border { Color = "blue" },
            BorderTop = new Border { Color = "blue" }
        });

        for (var attempt = 0; attempt < 2; attempt++)
        {
            sheet.GetFormat(2, 2).BorderLeft!.Color.Should().Be("blue");
            sheet.GetFormat(2, 2).BorderLeft!.Width.Should().Be(2);
            sheet.GetFormat(2, 2).BorderTop!.Color.Should().Be("blue");
            // the cells before the region, and those inside it, give up their side of the edge
            sheet.GetFormat(2, 1).BorderRight.Should().BeNull();
            sheet.GetFormat(2, 2).BorderRight.Should().BeNull();
            sheet.GetFormat(1, 2).BorderBottom.Should().BeNull();
            sheet.GetFormat(2, 2).BorderBottom.Should().BeNull();
            // edges the format says nothing about are untouched
            sheet.GetFormat(1, 1).BorderLeft!.Color.Should().Be("red");
            sheet.GetFormat(1, 1).BorderTop!.Color.Should().Be("red");
            sheet.GetFormat(4, 4).BorderRight!.Color.Should().Be("red");
            sheet.GetFormat(4, 4).BorderBottom!.Color.Should().Be("red");
            sheet.GetFormat(2, 2).BackgroundColor.Should().Be("yellow");
            sheet.Commands.Undo();
            for (var row = 0; row < 5; row++)
                for (var col = 0; col < 5; col++)
                {
                    var format = sheet.GetFormat(row, col);
                    format.BorderLeft!.Color.Should().Be("red");
                    format.BorderTop!.Color.Should().Be("red");
                    format.BorderRight!.Color.Should().Be("red");
                    format.BorderBottom!.Color.Should().Be("red");
                    format.BackgroundColor.Should().Be("yellow");
                }
            if (attempt == 0)
                sheet.Commands.Redo();
        }
    }

    [Test]
    public void Setting_All_Borders_Clears_Only_The_Neighbours_Around_The_Region()
    {
        var sheet = new Sheet(5, 5);
        var red = new Border { Width = 1, Color = "red" };
        sheet.SetFormat(sheet.Region, new CellFormat
        {
            BorderLeft = red, BorderTop = red, BorderRight = red, BorderBottom = red
        });
        var blue = new Border { Width = 1, Color = "blue" };
        sheet.SetFormat(new Region(1, 2, 1, 2), new CellFormat
        {
            BorderLeft = blue, BorderTop = blue, BorderRight = blue, BorderBottom = blue
        });

        sheet.GetFormat(1, 1).BorderRight!.Color.Should().Be("blue");
        sheet.GetFormat(1, 2).BorderLeft!.Color.Should().Be("blue");
        sheet.GetFormat(1, 0).BorderRight.Should().BeNull();
        sheet.GetFormat(1, 3).BorderLeft.Should().BeNull();
        sheet.GetFormat(0, 1).BorderBottom.Should().BeNull();
        sheet.GetFormat(3, 1).BorderTop.Should().BeNull();
        sheet.GetFormat(1, 3).BorderRight!.Color.Should().Be("red");
    }

    [Test]
    public void Clearing_A_Border_Clears_The_Neighbours_Side_Of_The_Edge()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new Region(1, 0), new CellFormat { BorderRight = new Border { Width = 1, Color = "red" } });
        sheet.SetFormat(new Region(1, 1), new CellFormat { BorderLeft = null });
        sheet.GetFormat(1, 0).BorderRight.Should().BeNull();
        sheet.Commands.Undo();
        sheet.GetFormat(1, 0).BorderRight!.Color.Should().Be("red");
    }

    [TestCase("cells")]
    [TestCase("rows")]
    [TestCase("columns")]
    public void Borders_At_The_Sheet_Edge_Write_Nothing_Outside_The_Sheet(string kind)
    {
        var sheet = new Sheet(3, 3);
        var border = new Border { Width = 1, Color = "red" };
        IRegion region = kind switch
        {
            "rows" => new RowRegion(0, 2),
            "columns" => new ColumnRegion(0, 2),
            _ => new Region(0, 2, 0, 2)
        };
        sheet.SetFormat(region, new CellFormat
        {
            BorderLeft = border, BorderTop = border, BorderRight = border, BorderBottom = border
        });

        sheet.GetFormat(0, 0).BorderLeft!.Color.Should().Be("red");
        sheet.GetFormat(0, 0).BorderTop!.Color.Should().Be("red");
        sheet.GetFormat(2, 2).BorderRight!.Color.Should().Be("red");
        sheet.Rows.Formats.GetAllIntervals().Should().OnlyContain(x => x.Start >= 0 && x.End <= 2);
        sheet.Columns.Formats.GetAllIntervals().Should().OnlyContain(x => x.Start >= 0 && x.End <= 2);
        sheet.Cells.GetFormatStore().GetAllDataRegions()
            .Should().OnlyContain(x => sheet.Region.Contains(x.Region));
    }

    [Test]
    public void Clearing_Neighbouring_Borders_Can_Be_Disabled()
    {
        var sheet = new Sheet(3, 3);
        sheet.SetFormat(new Region(1, 0), new CellFormat { BorderRight = new Border { Width = 1, Color = "red" } });
        var command = new SetFormatCommand(new Region(1, 1), new CellFormat
        {
            BorderLeft = new Border { Width = 2, Color = "blue" }
        }, clearSurroundingBorders: false);

        sheet.Commands.ExecuteCommand(command);
        sheet.GetFormat(1, 0).BorderRight!.Color.Should().Be("red");
        sheet.Commands.Undo();
        sheet.GetFormat(1, 1).BorderLeft.Should().BeNull();
    }

    [Test]
    public void Set_Format_And_Undo_Removes_All_Formats()
    {
        var red = new CellFormat() { BackgroundColor = "red" };
        var blue = new CellFormat() { BackgroundColor = "blue" };
        var sheet = new Sheet(100, 100);
        sheet.Range("B:C").Format = red;
        sheet.Range("5:12").Format = blue;
        sheet.Range("7:10").Format = red;
        sheet.Range("A3:D13").Format = red;
        sheet.Commands.Undo();
        sheet.Commands.Undo();
        sheet.Commands.Undo();
        sheet.Commands.Undo();
        sheet.Cells.GetFormatStore().GetAllDataRegions().Should().BeEmpty();
    }

    private List<DataRegion<CellFormat>> GetSnapshot(Sheet sheet)
    {
        var snapshot = sheet.Cells.GetFormatStore()
            .GetAllDataRegions()
            .Select(x => new DataRegion<CellFormat>(x.Data.Clone(), x.Region.Clone()))
            .OrderBy(x => x.Region.Left)
            .ThenBy(x => x.Region.Top)
            .ThenBy(x => x.Region.Right)
            .ThenBy(x => x.Region.Bottom);
        return snapshot.ToList();
    }

    private IRegion Rand_Range(Random r)
    {
        var n = r.Next(0, 4);
        if (n == 0)
            return new ColumnRegion(r.Next(0, 10), r.Next(0, 10));
        if (n == 1)
            return new RowRegion(r.Next(0, 10), r.Next(0, 10));
        return new Region(r.Next(0, 10), r.Next(0, 10), r.Next(0, 10), r.Next(0, 10));
    }

    [Test]
    public void Setting_Border_Beside_Cells_Without_Borders_Stores_No_Format_On_Them()
    {
        var sheet = new Sheet(10, 10);
        sheet.SetFormat(new ColumnRegion(3), new CellFormat { BorderTop = new Border { Color = "blue" } });
        sheet.SetFormat(new Region(2, 4, 6, 6), new CellFormat { BorderLeft = new Border { Color = "blue" } });

        sheet.Cells.GetFormatData(new ColumnRegion(3)).Should().BeEmpty();
        sheet.Cells.GetFormatData(new ColumnRegion(5)).Should().BeEmpty();
        sheet.Columns.Formats.Get(3)!.BorderTop!.Color.Should().Be("blue");
    }

    [Test]
    public void Setting_Column_Format_Over_Many_Formats_Raises_One_Format_Changed_And_Undoes()
    {
        var sheet = new Sheet(20, 10);
        for (var row = 0; row < 20; row += 2)
            sheet.SetFormat(new RowRegion(row), new CellFormat { BackgroundColor = "yellow" });
        for (var row = 0; row < 20; row++)
            sheet.SetFormat(new Region(row, 3), new CellFormat { FontWeight = "bold" });

        var events = new List<IRegion>();
        sheet.Cells.FormatChanged += (_, e) => events.Add(e.Region);

        sheet.SetFormat(new ColumnRegion(3), new CellFormat { BorderLeft = new Border { Color = "blue" } });

        events.Should().ContainSingle().Which.Should().BeOfType<ColumnRegion>();
        for (var row = 0; row < 20; row++)
        {
            var format = sheet.GetFormat(row, 3);
            format.BorderLeft!.Color.Should().Be("blue");
            format.FontWeight.Should().Be("bold");
            format.BackgroundColor.Should().Be(row % 2 == 0 ? "yellow" : null);
        }

        sheet.Commands.Undo();

        for (var row = 0; row < 20; row++)
        {
            var format = sheet.GetFormat(row, 3);
            format.BorderLeft.Should().BeNull();
            format.FontWeight.Should().Be("bold");
        }
    }
}
