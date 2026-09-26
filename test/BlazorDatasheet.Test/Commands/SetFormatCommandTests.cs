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
    public void Partial_Borders_Update_Shared_Edges_And_Undo_Redo_Restores_Formats(string kind)
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
            if (kind != "rows")
            {
                sheet.GetFormat(2, 1).BorderRight!.Color.Should().Be("blue");
                sheet.GetFormat(2, 1).BorderRight!.Width.Should().Be(2);
            }
            if (kind != "columns")
            {
                sheet.GetFormat(1, 2).BorderBottom!.Color.Should().Be("blue");
                sheet.GetFormat(1, 2).BorderBottom!.Width.Should().Be(2);
            }
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
    public void Border_Propagation_Can_Be_Disabled()
    {
        var sheet = new Sheet(3, 3);
        var command = new SetFormatCommand(new Region(1, 1), new CellFormat
        {
            BorderLeft = new Border { Width = 2, Color = "red" },
            BorderTop = new Border { Width = 2, Color = "red" }
        }, clearSurroundingBorders: false);

        sheet.Commands.ExecuteCommand(command);
        sheet.GetFormat(1, 0).BorderRight.Should().BeNull();
        sheet.GetFormat(0, 1).BorderBottom.Should().BeNull();
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
}