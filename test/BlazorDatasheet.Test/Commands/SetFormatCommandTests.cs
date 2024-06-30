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
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetFormatCommandTests
{
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