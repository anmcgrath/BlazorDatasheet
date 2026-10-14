using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class ArrayConstantTests
{
    private Evaluator _evaluator;
    private TestEnvironment _env;
    private Parser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _evaluator = new Evaluator(_env);
        _parser = new Parser(_env);
    }

    [Test]
    public void Array_Constant_Parses_As_Array_Expression()
    {
        var formula = _parser.Parse("={1,2,3}");
        var res = (CellValue[][])_evaluator.Evaluate(formula).Data!;
        res.Should().BeOfType<CellValue[][]>();
        res.Length.Should().Be(1);
        res[0].Length.Should().Be(3);
        res[0][0].Data.Should().Be(1);
        res[0][1].Data.Should().Be(2);
        res[0][2].Data.Should().Be(3);
    }

    [Test]
    public void Multi_Row_Array_Constant_Parses_As_Array_Expression()
    {
        var formula = _parser.Parse("={1,2;3,4}");
        var res = (CellValue[][])_evaluator.Evaluate(formula).Data!;
        res.Should().BeOfType<CellValue[][]>();
        res.Length.Should().Be(2);
        res[0].Length.Should().Be(2);
        res[1].Length.Should().Be(2);
        res[0][0].Data.Should().Be(1);
        res[0][1].Data.Should().Be(2);
        res[1][0].Data.Should().Be(3);
        res[1][1].Data.Should().Be(4);
    }

    [Test]
    public void Invalid_Row_Length_Fails_Validation()
    {
        var formula = _parser.Parse("={1,2;3,4,5}");
        formula.Errors.Should().HaveCountGreaterThan(0);
    }
    [Test]
    public void Signed_Logical_And_Error_Elements_Evaluate_And_Round_Trip()
    {
        var tree = _parser.Parse("={-1,+2;TRUE,FALSE;#N/A,#DIV/0!}");
        tree.Errors.Should().BeEmpty();
        var values = (CellValue[][])_evaluator.Evaluate(tree).Data!;
        values[0][0].NumberValue.Should().Be(-1);
        values[0][1].NumberValue.Should().Be(2);
        values[1][0].LogicalValue.Should().BeTrue();
        values[1][1].LogicalValue.Should().BeFalse();
        ((FormulaError)values[2][0].Data!).ErrorType.Should().Be(ErrorType.Na);
        ((FormulaError)values[2][1].Data!).ErrorType.Should().Be(ErrorType.Div0);

        var reparsed = _parser.Parse("=" + tree.Root.ToExpressionText());
        reparsed.Errors.Should().BeEmpty();
        _evaluator.Evaluate(reparsed).Data.Should().BeEquivalentTo(values);
    }

    [TestCase("={A1;2}")]
    [TestCase("={1+2;3}")]
    [TestCase("={-TRUE;2}")]
    [TestCase("={--1;2}")]
    public void Array_Elements_Must_Be_Constants(string formula)
    {
        _parser.Parse(formula).Errors.Should().NotBeEmpty();
    }
}