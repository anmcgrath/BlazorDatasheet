using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using FluentAssertions;
using NUnit.Framework;
using Parser = BlazorDatasheet.Formula.Core.Interpreter.Parsing.Parser;

namespace BlazorDatasheet.Test.Formula;

public class EvaluatorReferenceOffsetTests
{
    private Evaluator _evaluator = null!;
    private TestEnvironment _env = null!;
    private Parser _parser = null!;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _evaluator = new Evaluator(_env);
        _parser = new Parser(_env);
    }

    private CellValue EvaluateWithOffset(string formulaString, int rowOffset, int colOffset,
        string sheetName = "Sheet1")
    {
        var formula = _parser.FromString(formulaString);
        return _evaluator.Evaluate(formula, new FormulaExecutionContext(),
            new FormulaEvaluationOptions(false, new ReferenceOffset(rowOffset, colOffset, sheetName)));
    }

    [Test]
    public void Relative_Reference_Is_Shifted_By_The_Offset()
    {
        _env.SetCellValue(2, 1, 10);
        EvaluateWithOffset("=A1", 2, 1).Data.Should().Be(10);
    }

    [Test]
    public void Fixed_Parts_Are_Not_Shifted()
    {
        _env.SetCellValue(2, 0, 10);
        EvaluateWithOffset("=$A1", 2, 1).Data.Should().Be(10);

        _env.SetCellValue(0, 1, 20);
        EvaluateWithOffset("=A$1", 2, 1).Data.Should().Be(20);
    }

    [Test]
    public void Offset_Above_The_Sheet_Is_A_Ref_Error()
    {
        _env.SetCellValue(0, 0, 10);
        var result = EvaluateWithOffset("=A1", -1, 0);
        result.ValueType.Should().Be(CellValueType.Error);
        ((FormulaError)result.Data!).ErrorType.Should().Be(ErrorType.Ref);
    }

    [Test]
    public void References_On_Another_Sheet_Are_Not_Shifted()
    {
        _env.SetCellValue(0, 0, 10);
        EvaluateWithOffset("=A1", 2, 1, "OtherSheet").Data.Should().Be(10);
    }

    [Test]
    public void Range_References_Are_Shifted()
    {
        _env.SetCellValue(1, 1, 1);
        _env.SetCellValue(2, 1, 2);
        _env.RegisterFunction(BlazorDatashet.Formula.Functions.Math.SumFunction.Descriptor);
        var result = _evaluator.Evaluate(_parser.FromString("=SUM(A1:A2)"), new FormulaExecutionContext(),
            new FormulaEvaluationOptions(false, new ReferenceOffset(1, 1, "Sheet1")));
        result.Data.Should().Be(3d);
    }
}
