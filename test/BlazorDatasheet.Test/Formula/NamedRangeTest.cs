using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class NamedRangeTest
{
    private Workbook _workbook;
    private Sheet _sheet;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet(100, 100);
    }

    [Test]
    public void Add_Named_Range_Success_When_Referenced()
    {
        _sheet.NamedRanges.Set("x", "A1");
        _sheet.Cells["A2"]!.Formula = "=x+1";
        _sheet.Cells["A1"]!.Value = 10;
        _sheet.Cells["A2"]!.Value.Should().Be(11);
    }

    [Test]
    public void Add_Named_Range_Success_When_Referenced_In_Alternate_Order()
    {
        _sheet.Cells["A1"]!.Value = 10;
        _sheet.Cells["A2"]!.Formula = "=x+1";
        _sheet.NamedRanges.Set("x", "A1");
        _sheet.Cells["A2"]!.Value.Should().Be(11);
    }

    [Test]
    public void Set_Named_Range_Fails_When_Invalid()
    {
        _sheet.NamedRanges.Set("x", "A_1");
        _sheet.Cells["A2"]!.Formula = "=x+1";
        _sheet.Cells["A2"]!.Value.Should().BeOfType<FormulaError>();
    }

    [Test]
    public void Cleared_Named_Range_Clears_Named_Range()
    {
        _sheet.NamedRanges.Set("x", "A1");
        _sheet.Cells["A1"]!.Value = 10;
        _sheet.Cells["A2"]!.Formula = "=x+1";
        _sheet.NamedRanges.Clear("x");
        _sheet.Cells["A2"]!.Value.Should().BeOfType<FormulaError>();
    }

    [Test]
    public void Set_Named_Range_String_Returns_Correct_String()
    {
        _sheet.NamedRanges.Set("x", "A1");
        _sheet.NamedRanges.GetRangeString("x").Should().Be("Sheet1!A1");
    }

    [Test]
    public void Named_Ranges_Changes_On_Row_Insert()
    {
        _sheet.NamedRanges.Set("x", "Sheet1!A1");
        _sheet.Rows.InsertAt(0);
        _sheet.NamedRanges.GetRangeString("x").Should().Be("Sheet1!A2");
    }

    [Test]
    public void Named_Ranges_Changes_On_Row_Remove()
    {
        _sheet.NamedRanges.Set("x", "A1:A2");
        _sheet.Rows.RemoveAt(0);
        _sheet.NamedRanges.GetRangeString("x").Should().Be("Sheet1!A1");
        _sheet.Commands.Undo();
        _sheet.NamedRanges.GetRangeString("x").Should().Be("Sheet1!A1:A2");
    }

    [Test]
    public void Named_Region_Selection_Gets_Correct_Name()
    {
        _sheet.NamedRanges.Set("x", "A1:A2");
        _sheet.NamedRanges.GetRegionName(_sheet.Range("A1:A2")!.Region).Should().Be("x");
    }
}