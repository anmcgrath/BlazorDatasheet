﻿using System;
using System.Drawing;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
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
            sheet.ConditionalFormats.Apply(new ColumnRegion(6),
                new CustomCf("=A1=\"Hello, World!\"", Color.Red));

            sheet.Cells.SetCellMetaData(5, 5, "test1", "testData");
            sheet.Cells.SetCellMetaData(5, 5, "test2", 5);
            sheet.Cells.SetCellMetaData(5, 5, "test3", true);


            var s = new SheetJsonSerializer(ConditionalFormatTypeResolver);
            var json = s.Serialize(sheet.Workbook);

            var deserializer = new SheetJsonDeserializer(ConditionalFormatTypeResolver);
            var wbDeserialized = deserializer.Deserialize(json);
            CompareSheets(sheet.Workbook, wbDeserialized);
        });
    }

    private Type? ConditionalFormatTypeResolver(string arg)
    {
        switch (arg)
        {
            case nameof(CustomCf):
                return typeof(CustomCf);
        }

        return null;
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

            for (int rowIndex = 0; rowIndex < sheet1.NumRows; rowIndex++)
            {
                sheet1.Rows[rowIndex].RowIndex.Should().Be(sheet2.Rows[rowIndex].RowIndex);
                sheet1.Rows[rowIndex].Height.Should().Be(sheet2.Rows[rowIndex].Height);
                sheet1.Rows[rowIndex].IsVisible.Should().Be(sheet2.Rows[rowIndex].IsVisible);
                sheet1.Rows[rowIndex].Heading.Should().Be(sheet2.Rows[rowIndex].Heading);
                sheet1.Rows[rowIndex].NonEmptyCells.Count().Should().Be(sheet2.Rows[rowIndex].NonEmptyCells.Count());

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

public class CustomCf : ConditionalFormatAbstractBase
{
    public string Formula { get; set; }
    public Color ColorIfTrue { get; set; }

    public CustomCf(string formula, Color colorIfTrue)
    {
        Formula = formula;
        ColorIfTrue = colorIfTrue;
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cform = sheet.FormulaEngine.ParseFormula(Formula, sheet.Name);
        var value = sheet.FormulaEngine.Evaluate(cform);
        if (value.ValueType == CellValueType.Logical && value.GetValue<bool>())
        {
            return new CellFormat()
            {
                BackgroundColor = System.Drawing.ColorTranslator.ToHtml(ColorIfTrue)
            };
        }

        return null;
    }
}