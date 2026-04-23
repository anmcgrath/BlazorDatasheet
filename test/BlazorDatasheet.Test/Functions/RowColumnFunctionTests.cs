using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;
using FluentAssertions;
using NUnit.Framework;
using Parser = BlazorDatasheet.Formula.Core.Interpreter.Parsing.Parser;

namespace BlazorDatasheet.Test.Functions;

public class RowColumnFunctionTests
{
    private TestEnvironment _env = null!;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _env.RegisterFunction(RowFunction.Descriptor);
        _env.RegisterFunction(ColumnFunction.Descriptor);
    }

    private object? Eval(string formulaString)
    {
        var eval = new Evaluator(_env);
        var parser = new Parser(_env);
        return eval.Evaluate(parser.FromString(formulaString)).Data;
    }

    [Test]
    public void Row_And_Column_Without_Caller_Return_Value_Error()
    {
        Eval("=ROW()").Should().BeOfType<FormulaError>()
            .Which.ErrorType.Should().Be(ErrorType.Value);
        Eval("=COLUMN()").Should().BeOfType<FormulaError>()
            .Which.ErrorType.Should().Be(ErrorType.Value);
    }

    [Test]
    public void Row_And_Column_With_Reference_Return_One_Based_Position()
    {
        Eval("=ROW(B3)").Should().Be(3d);
        Eval("=COLUMN(B3)").Should().Be(2d);
        Eval("=ROW(B3:D9)").Should().Be(3d);
        Eval("=COLUMN(B3:D9)").Should().Be(2d);
    }

    [Test]
    public void Row_And_Column_With_Non_Reference_Return_Value_Error()
    {
        Eval("=ROW(123)").Should().BeOfType<FormulaError>()
            .Which.ErrorType.Should().Be(ErrorType.Value);
        Eval("=COLUMN(\"x\")").Should().BeOfType<FormulaError>()
            .Which.ErrorType.Should().Be(ErrorType.Value);
    }

    [Test]
    public void Row_And_Column_Without_Arguments_Use_Calling_Cell()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);

        sheet.Cells[2, 1].Formula = "=ROW()";
        sheet.Cells[4, 3].Formula = "=COLUMN()";

        sheet.Cells[2, 1].Value.Should().Be(3d);
        sheet.Cells[4, 3].Value.Should().Be(4d);
    }

    [Test]
    public void Row_And_Column_In_Named_Formula_Without_Caller_Return_Value_Error()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells["A1"]!.Formula = "=x";

        sheet.FormulaEngine.SetVariable("x", "=ROW()");

        sheet.Cells.GetCellValue(0, 0).GetValue<FormulaError>()!.ErrorType.Should().Be(ErrorType.Value);

        sheet.FormulaEngine.SetVariable("x", "=COLUMN()");

        sheet.Cells.GetCellValue(0, 0).GetValue<FormulaError>()!.ErrorType.Should().Be(ErrorType.Value);
    }
}
