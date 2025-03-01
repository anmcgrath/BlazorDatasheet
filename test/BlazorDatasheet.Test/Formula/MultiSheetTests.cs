using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatashet.Formula.Functions.Math;
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
        _sheet2.Cells["A2"]!.Value = "New";
        _sheet1.Cells["A1"]!.Value.Should().Be("New");
    }

    [Test]
    public void Auto_Fill_Ref_Updates_Multi_Sheet_Ref()
    {
        _sheet1.Cells["A1"]!.Formula = "='Sheet2'!A2";
        _sheet1.Cells.CopyImpl(new Region(0, 0), new Region(1, 0), new CopyOptions());
        _sheet1.Cells["A2"]!.Formula.Should().Be("='Sheet2'!A3");
    }

    [Test]
    public void Range_Ref_With_Sheet_Infront_Of_Both_Cells_References_Ok()
    {
        var parsedFormula = _sheet1.FormulaEngine.ParseFormula("=sum(Sheet2!A1:Sheet2!A2)");
        parsedFormula.ExpressionTree.Errors.Should().BeEmpty();
        parsedFormula.References.Count().Should().Be(1);
        parsedFormula.References.First().Should().BeOfType<RangeReference>();
        parsedFormula.References.First().SheetName.Should().Be("Sheet2");
    }

    [Test]
    public void Range_Ref_Across_Sheets_Should_Result_In_Error()
    {
        var parsedFormula = _sheet1.FormulaEngine.ParseFormula("=sum(Sheet1!A1:Sheet2!:A2)");
        parsedFormula.ExpressionTree.Errors.Should().NotBeEmpty();
    }

    [Test]
    public void Invalid_Reference_Should_Result_In_Error()
    {
        _sheet1.Cells["A1"]!.Formula = "=Sheet3!A1"; // Sheet3 does not exist
        _sheet1.Cells["A1"]!.Value.Should().BeOfType<FormulaError>();
    }

    [Test]
    public void Multi_Sheet_Col_Ref_Evaluate_Correctly()
    {
        var r = _sheet1.Range("Sheet2!A:Sheet2!B");
        r.Region.Should().BeOfType<ColumnRegion>();
        r.Region.Left.Should().Be(0);
        r.Region.Right.Should().Be(1);
    }

    [Test]
    public void Multi_Sheet_Row_Ref_Evaluate_Correctly()
    {
        var r = _sheet1.Range("Sheet2!4:Sheet2!5");
        r.Region.Should().BeOfType<RowRegion>();
        r.Region.Top.Should().Be(3);
        r.Region.Bottom.Should().Be(4);
    }

    [Test]
    public void Multi_Sheet_Range_Operator_Works_Correctly()
    {
        var r = _sheet1.Range("Sheet2!A1:Sheet2!B2:Sheet2!C3");
        r.Region.Should().BeOfType<Region>();
        r.Region.Left.Should().Be(0);
        r.Region.Right.Should().Be(2);
        r.Region.Top.Should().Be(0);
        r.Region.Bottom.Should().Be(2);
    }

    [Test]
    public void Multi_Sheet_Range_Intersection_Evaluates_Correctly()
    {
        _sheet2.Range("Sheet2!A1:Sheet2!B2:Sheet2!C3")!.Value = 2;
        _sheet1.Cells["A1"]!.Formula = "=sum(Sheet2!A1:Sheet2!B2:Sheet2!C3)";
        _sheet1.Cells["A1"]!.Value.Should().Be(18);
    }
}