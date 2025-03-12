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
            sheet.Range("D:D")!.Type = "boolean";

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

            var rows1 = Enumerable.Range(0, sheet1.Rows.NonEmpty.Count()).Select(x => new SheetRow(x, sheet1))
                .ToArray();
            var rows2 = Enumerable.Range(0, sheet2.Rows.NonEmpty.Count()).Select(x => new SheetRow(x, sheet2))
                .ToArray();

            for (int rowIndex = 0; rowIndex < rows1.Length; rowIndex++)
            {
                rows1[rowIndex].RowIndex.Should().Be(rows2[rowIndex].RowIndex);
                rows1[rowIndex].Height.Should().Be(rows2[rowIndex].Height);
                rows1[rowIndex].IsVisible.Should().Be(rows2[rowIndex].IsVisible);
                rows1[rowIndex].Heading.Should().Be(rows2[rowIndex].Heading);
                rows1[rowIndex].NonEmptyCells.Count().Should().Be(rows2[rowIndex].NonEmptyCells.Count());

                for (int colIndex = 0; colIndex < sheet1.NumCols; colIndex++)
                {
                    var cell1 = sheet1.Cells.GetCell(rowIndex, colIndex);
                    var cell2 = sheet2.Cells.GetCell(rowIndex, colIndex);

                    cell1.Col.Should().Be(cell2.Col);
                    cell1.CellValue.Should().BeEquivalentTo(cell2.CellValue);
                    cell1.Formula.Should().Be(cell2.Formula);
                    cell1.Format.Should().BeEquivalentTo(cell2.Format);
                    cell1.IsValid.Should().Be(cell2.IsValid);
                    cell1.ValueType.Should().Be(cell2.ValueType);
                    cell1.MetaData.Should().BeEquivalentTo(cell2.MetaData);
                    cell1.Type.Should().BeEquivalentTo(cell2.Type);
                }
            }
        }
    }
}