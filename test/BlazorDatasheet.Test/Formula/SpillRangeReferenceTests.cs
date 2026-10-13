using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatashet.Formula.Functions.Math;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class SpillRangeReferenceTests
{
    private Evaluator _evaluator;
    private TestEnvironment _env;
    private Parser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _env.RegisterFunction(SumFunction.Descriptor);
        _evaluator = new Evaluator(_env);
        _parser = new Parser(_env);
    }

    [Test]
    public void Spill_Range_Parses_To_A_Spill_Range_Reference()
    {
        var formula = _parser.FromString("=SUM(A1#)");
        formula.IsValid().Should().BeTrue();
        var reference = formula.References.Single();
        reference.Should().BeOfType<SpillRangeReference>();
        reference.Kind.Should().Be(ReferenceKind.SpillRange);
        reference.Region.Top.Should().Be(0);
        reference.Region.Left.Should().Be(0);
    }

    [Test]
    [TestCase("=SUM(A1#)")]
    [TestCase("=$A$1#")]
    [TestCase("='My Sheet'!B2#*2")]
    public void Spill_Range_Round_Trips_To_Text(string formulaString)
    {
        var formula = _parser.FromString(formulaString);
        formula.IsValid().Should().BeTrue();
        formula.ToFormulaString().Should().Be(formulaString);
    }

    [Test]
    public void Spill_Range_Shifts_With_The_Anchor_Unless_Fixed()
    {
        var formula = _parser.FromString("=A1#+$A$1#");
        formula.ShiftReferences(1, 1, null);
        formula.ToFormulaString().Should().Be("=B2#+$A$1#");
    }

    [Test]
    public void Spill_Range_Over_A_Range_Is_Invalid()
    {
        _parser.FromString("=A1#:B2").IsValid().Should().BeFalse();
    }

    [Test]
    public void Spill_Range_Without_A_Spill_Is_Ref_Error()
    {
        var result = _evaluator.Evaluate(_parser.FromString("=A1#"));
        result.IsError().Should().BeTrue();
        ((FormulaError)result.Data!).ErrorType.Should().Be(ErrorType.Ref);
    }

    [Test]
    public void Spill_Range_Evaluates_To_The_Values_In_The_Spill_Region()
    {
        _env.SetCellValue(0, 0, 1);
        _env.SetCellValue(1, 0, 2);
        _env.SetCellValue(2, 0, 3);
        _env.SetSpillRegion(0, 0, new Region(0, 2, 0, 0));

        _evaluator.Evaluate(_parser.FromString("=SUM(A1#)")).Data.Should().Be(6.0);

        var array = (CellValue[][])_evaluator.Evaluate(_parser.FromString("=A1#*2")).Data!;
        array.Select(r => r[0].Data).Should().Equal(2.0, 4.0, 6.0);
    }
}
