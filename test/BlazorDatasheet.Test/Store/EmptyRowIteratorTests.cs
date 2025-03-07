using System;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class EmptyRowIteratorTests
{
    private Sheet _sheet;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(100, 100);
    }

    [Test]
    public void No_Rows_Doesnt_Iterate()
    {
        _sheet.Rows.NonEmpty.Count().Should().Be(0);
    }

    [Test]
    public void Row_Data_Count_Correct()
    {
        _sheet.Cells["A1"]!.Formula = "=1";
        _sheet.Cells["A5"]!.Value = "1";
        _sheet.Rows.NonEmpty.Count().Should().Be(2);
    }

    [Test]
    public void Row_Data_Returns_Correct_Row_Indices()
    {
        _sheet.Cells["A1"]!.Value = "1";
        _sheet.Cells["A5"]!.Formula = "=1";
        _sheet.Cells["B5"]!.Formula = "=1";
        _sheet.Rows.NonEmpty.Select(r => r.RowIndex).Should().BeEquivalentTo(new[] { 0, 4 });
        _sheet.Rows.NonEmpty.Select(r => r.Row).Should().BeEquivalentTo(new[] { 1, 5 });
    }

    [Test]
    public void Row_Data_With_Row_Formats_Returns_Those_Rows()
    {
        _sheet.SetFormat(new RowRegion(5, 6), new CellFormat());
        _sheet.Rows.NonEmpty.Count().Should().Be(2);
    }

    [Test]
    public void Row_Data_With_Columns_Count_Correct()
    {
        _sheet.Cells["A1"]!.Formula = "=1";
        _sheet.Cells["A5"]!.Value = "1";
        _sheet.Rows.NonEmpty.First().NonEmptyCells.Count().Should().Be(1);
        _sheet.Rows.NonEmpty.Last().NonEmptyCells.Count().Should().Be(1);
    }
}