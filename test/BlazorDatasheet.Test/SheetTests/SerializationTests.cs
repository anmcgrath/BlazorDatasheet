using System;
using System.Drawing;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Serialization.Json;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SerializationTests
{
    [Test]
    public void SerializationTest()
    {
        Assert.DoesNotThrow(() =>
        {
            var sheet = new Sheet(10, 10);
            sheet.Cells["A1"]!.Value = "Hello, World!";
            sheet.Cells["B2"]!.Value = 42;
            sheet.Cells["A3"]!.Value = 3.14;
            sheet.Cells["A4"]!.Value = true;
            sheet.Cells["A5"]!.Value = new DateTime(2021, 1, 1);
            sheet.Cells["A7"]!.Formula = "=sum(A2:A3)";

            sheet.ConditionalFormats.Apply(new ColumnRegion(5),
                new NumberScaleConditionalFormat(Color.Aqua, Color.Red));

            sheet.Cells.SetCellMetaData(5, 5, "test1", "testData");
            sheet.Cells.SetCellMetaData(5, 5, "test2", 5);
            sheet.Cells.SetCellMetaData(5, 5, "test3", true);


            var s = new SheetJsonSerializer();
            var json = s.Serialize(sheet.Workbook);

            var wbDeserialized = new SheetJsonDeserializer().Deserialize(json);
            CompareSheets(sheet.Workbook, wbDeserialized);
        });
    }

    private void CompareSheets(Workbook wb1, Workbook wb2)
    {
        var sheets1 = wb1.Sheets.ToArray();
        var sheets2 = wb2.Sheets.ToArray();
        for (int sIndex = 0; sIndex < sheets1.Length; sIndex++)
        {
            var sheet1 = sheets1[sIndex];
            var sheet2 = sheets2[sIndex];
            sheet2.NumRows.Should().Be(sheet1.NumRows);
            sheet2.NumCols.Should().Be(sheet1.NumCols);
            sheet2.Rows.NonEmpty.Count().Should().Be(sheet1.Rows.NonEmpty.Count());
            sheet2.Columns.NonEmpty.Count().Should().Be(sheet1.Columns.NonEmpty.Count());

            var rows1 = sheet1.Rows.NonEmpty.ToArray();
            var rows2 = sheet2.Rows.NonEmpty.ToArray();

            for (int i = 0; i < rows1.Length; i++)
            {
                rows1[i].RowIndex.Should().Be(rows2[i].RowIndex);
                rows1[i].Height.Should().Be(rows2[i].Height);
                rows1[i].IsVisible.Should().Be(rows2[i].IsVisible);
                rows1[i].Heading.Should().Be(rows2[i].Heading);
                rows1[i].NonEmptyCells.Count().Should().Be(rows2[i].NonEmptyCells.Count());
                for (int c = 0; c < rows1[i].NonEmptyCells.Count(); c++)
                {
                    var cell1 = rows1[i].NonEmptyCells.ElementAt(c);
                    var cell2 = rows2[i].NonEmptyCells.ElementAt(c);
                    cell1.Col.Should().Be(cell2.Col);
                    cell1.CellValue.Should().BeEquivalentTo(cell2.CellValue);
                    cell1.Formula.Should().Be(cell2.Formula);
                    cell1.Format.Should().BeEquivalentTo(cell2.Format);
                    cell1.IsValid.Should().Be(cell2.IsValid);
                    cell1.ValueType.Should().Be(cell2.ValueType);
                    cell1.GetMetaData("test1").Should().Be(cell2.GetMetaData("test1"));
                    cell1.GetMetaData("test2").Should().Be(cell2.GetMetaData("test2"));
                    cell1.GetMetaData("test3").Should().Be(cell2.GetMetaData("test3"));
                }
            }
        }
    }
}