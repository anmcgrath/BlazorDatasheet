using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class MultiSheetTests
{
    private Workbook _workbook;
    private Sheet _sheet1;
    private Sheet _sheet2;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet1 = _workbook.AddSheet(10, 10);
        _sheet2 = _workbook.AddSheet(10, 10);
    }

    [Test]
    public void Sheet_References_Other_Sheet_In_Workbook()
    {
        _sheet2.Cells["A2"]!.Value = "Test";
        _sheet1.Cells["A1"]!.Formula = "=Sheet2!A2";
        _sheet1.Cells["A1"]!.Value.Should().Be("Test");
    }

    [Test]
    public void Formula_Recalculated_When_Other_Sheet_Value_Changes()
    {
        _sheet1.Cells["A1"]!.Formula = "=Sheet2!A2";
        _sheet2.Cells["A2"]!.Value = "Test";
        _sheet1.Cells["A1"]!.Value.Should().Be("Test");
    }

    [Test]
    public void Insert_Row_In_Other_Sheet_Updates_References()
    {
        _sheet1.Cells["A1"]!.Formula = "='Sheet2'!A2";
        _sheet2.Cells["A2"]!.Value = "Test";
        _sheet2.Rows.InsertAt(0);
        _sheet1.Cells["A1"]!.Value.Should().Be("Test");
        _sheet1.Cells["A1"]!.Formula.Should().Be("='Sheet2'!A3");

        _sheet2.Cells["A3"]!.Value = "Test2";
        _sheet1.Cells["A1"]!.Value.Should().Be("Test2");
    }

    [Test]
    public void Insert_Row_In_Same_Sheet_Keeps_References()
    {
        _sheet1.Cells["A1"]!.Formula = "='Sheet2'!A2";
        _sheet2.Cells["A2"]!.Value = "Test";

        // Note sheet1 this time
        _sheet1.Rows.InsertAt(0);
        _sheet1.Cells["A2"]!.Formula.Should().Be("='Sheet2'!A2");
        _sheet1.Cells["A2"]!.Value.Should().Be("Test");

        _sheet2.Cells["A2"]!.Value = "Test2";
        _sheet1.Cells["A2"]!.Value.Should().Be("Test2");
    }

    [Test]
    public void Remove_Row_In_Other_Sheet_Keeps_References()
    {
        _sheet1.Cells["A3"]!.Formula = "='Sheet2'!A2";
        _sheet2.Cells["A2"]!.Value = "Test";

        // Note sheet1 this time
        _sheet2.Rows.RemoveAt(0);
        _sheet1.Cells["A3"]!.Formula.Should().Be("='Sheet2'!A1");
        _sheet1.Cells["A3"]!.Value.Should().Be("Test");

        _sheet2.Cells["A1"]!.Value = "Test2";
        _sheet1.Cells["A3"]!.Value.Should().Be("Test2");
    }

    [Test]
    public void Remove_Row_In_Same_Sheet_Keeps_References()
    {
        _sheet1.Cells["A3"]!.Formula = "='Sheet2'!A2";
        _sheet2.Cells["A2"]!.Value = "Test";

        // Note sheet1 this time
        _sheet1.Rows.RemoveAt(0);
        _sheet1.Cells["A2"]!.Formula.Should().Be("='Sheet2'!A2");
        _sheet1.Cells["A2"]!.Value.Should().Be("Test");

        _sheet2.Cells["A2"]!.Value = "Test2";
        _sheet1.Cells["A2"]!.Value.Should().Be("Test2");
    }

    [Test]
    public void Rename_Sheet_Renames_Refs()
    {
        _sheet1.Cells["A1"]!.Formula = "='Sheet2'!A2";
        _workbook.RenameSheet("Sheet2", "Renamed");
        _sheet1.Cells["A1"]!.Formula.Should().Be("='Renamed'!A2");
    }
}