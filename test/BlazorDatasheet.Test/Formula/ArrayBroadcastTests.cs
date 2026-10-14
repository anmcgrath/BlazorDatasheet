using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Functions.Logical;
using BlazorDatashet.Formula.Functions.Math;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class ArrayBroadcastTests
{
    private Evaluator _evaluator;
    private TestEnvironment _env;
    private Parser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _env.RegisterFunction(SumFunction.Descriptor);
        _env.RegisterFunction(SinFunction.Descriptor);
        _env.RegisterFunction(IfFunction.Descriptor);
        _evaluator = new Evaluator(_env);
        _parser = new Parser(_env);
        _env.SetCellValue(0, 0, 1);
        _env.SetCellValue(1, 0, 2);
        _env.SetCellValue(2, 0, 3);
        _env.SetCellValue(0, 1, 10);
        _env.SetCellValue(1, 1, 20);
        _env.SetCellValue(2, 1, 30);
    }

    private CellValue[][] EvalArray(string formula)
    {
        var result = _evaluator.Evaluate(_parser.Parse(formula));
        result.IsArray().Should().BeTrue($"{formula} should give an array, not {result.ValueType}");
        return (CellValue[][])result.Data!;
    }

    private static object?[][] Data(CellValue[][] array) =>
        array.Select(r => r.Select(c => c.Data).ToArray()).ToArray();

    [Test]
    public void Range_Times_Scalar_Lifts_Over_Each_Cell()
    {
        var res = EvalArray("=A1:A3*2");
        Data(res).Should().BeEquivalentTo(new object[][] { [2.0], [4.0], [6.0] });
    }

    [Test]
    public void Range_Plus_Range_Lifts_Element_Wise()
    {
        var res = EvalArray("=A1:A3+B1:B3");
        Data(res).Should().BeEquivalentTo(new object[][] { [11.0], [22.0], [33.0] });
    }

    [Test]
    public void Row_Times_Column_Gives_Outer_Product()
    {
        var res = EvalArray("={1,2,3}*{1;2}");
        res.Length.Should().Be(2);
        res[0].Length.Should().Be(3);
        Data(res).Should().BeEquivalentTo(new object[][] { [1.0, 2.0, 3.0], [2.0, 4.0, 6.0] });
    }

    [Test]
    public void Mismatched_Lengths_Give_Na_Outside_The_Shorter()
    {
        var res = EvalArray("={1,2}+{1,2,3}");
        res[0][0].Data.Should().Be(2.0);
        res[0][1].Data.Should().Be(4.0);
        res[0][2].IsError().Should().BeTrue();
        ((FormulaError)res[0][2].Data!).ErrorType.Should().Be(ErrorType.Na);
    }

    [Test]
    public void Unary_Minus_Lifts()
    {
        Data(EvalArray("=-A1:A2")).Should().BeEquivalentTo(new object[][] { [-1.0], [-2.0] });
    }

    [Test]
    public void Concat_Lifts()
    {
        Data(EvalArray("=A1:A2&\"x\"")).Should().BeEquivalentTo(new object[][] { ["1x"], ["2x"] });
    }

    [Test]
    public void Comparison_Lifts_To_Logical_Array()
    {
        var res = EvalArray("=A1:A3>1");
        res.Select(r => r[0].ValueType).Should().AllBeEquivalentTo(CellValueType.Logical);
        res.Select(r => r[0].LogicalValue).Should().Equal(false, true, true);
    }

    [Test]
    public void Sum_Of_Lifted_Expression_Reduces_To_Scalar()
    {
        var res = _evaluator.Evaluate(_parser.Parse("=SUM(A1:A3*2)"));
        res.Data.Should().Be(12.0);
    }

    [Test]
    public void Scalar_Function_Lifts_Over_Array_Argument()
    {
        var res = EvalArray("=SIN({0,0})");
        Data(res).Should().BeEquivalentTo(new object[][] { [0.0, 0.0] });
    }

    [Test]
    public void If_Lifts_Over_Logical_Array()
    {
        var res = EvalArray("=IF(A1:A3>1,\"y\",\"n\")");
        Data(res).Should().BeEquivalentTo(new object[][] { ["n"], ["y"], ["y"] });
    }

    [Test]
    public void If_Lifts_Array_Values_Alongside_The_Condition()
    {
        var res = EvalArray("=IF({1;0},{1;2},{3;4})");
        Data(res).Should().BeEquivalentTo(new object[][] { [1.0], [4.0] });
    }

    [Test]
    public void Element_Errors_Stay_In_Their_Element()
    {
        var res = EvalArray("={1,\"a\"}*2");
        res[0][0].Data.Should().Be(2.0);
        res[0][1].IsError().Should().BeTrue();
        ((FormulaError)res[0][1].Data!).ErrorType.Should().Be(ErrorType.Value);
    }

    [Test]
    public void Scalar_Operations_Are_Unchanged()
    {
        _evaluator.Evaluate(_parser.Parse("=A1*2+B1")).Data.Should().Be(12.0);
        _evaluator.Evaluate(_parser.Parse("=SIN(0)")).Data.Should().Be(0.0);
    }
    [TestCase("=IF({1;0},10,B1)")]
    [TestCase("=IF({TRUE;FALSE},10,B1)")]
    public void Lifted_If_Resolves_Scalar_Cell_References(string formula)
    {
        Data(EvalArray(formula)).Should().BeEquivalentTo(new object[][] { [10.0], [10.0] });
    }

    [Test]
    public void Lifted_References_Are_Resolved_Before_An_Aggregate_Consumes_Them()
    {
        _evaluator.Evaluate(_parser.Parse("=SUM(IF({1;0},1,B1))")).Data.Should().Be(11.0);
    }

    [Test]
    public void Function_Array_References_Are_Resolved_Without_Mutating_The_Source()
    {
        var cell = new CellReference(0, 1, false, false);
        var source = new CellValue[][] { [CellValue.Reference(cell)] };
        _env.RegisterFunction(new FunctionDescriptor("CUSTOM", [], (_, _) => CellValue.Array(source)));

        EvalArray("=CUSTOM()")[0][0].Data.Should().Be(10.0);
        source[0][0].ValueType.Should().Be(CellValueType.Reference);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Invalid_Function_Array_Results_Return_Calc(bool nested)
    {
        var source = nested
            ? new CellValue[][] { [CellValue.Array([[CellValue.Number(1)]])] }
            : new CellValue[][] { [CellValue.Number(1)], [CellValue.Number(2), CellValue.Number(3)] };
        _env.RegisterFunction(new FunctionDescriptor("CUSTOM", [], (_, _) => CellValue.Array(source)));
        var result = _evaluator.Evaluate(_parser.Parse("=SUM(CUSTOM())"));
        ((FormulaError)result.Data!).ErrorType.Should().Be(ErrorType.Calc);
    }

    [Test]
    public void Array_Shaped_Any_Parameter_Is_Passed_Whole_During_Lifting()
    {
        _env.RegisterFunction(new FunctionDescriptor("CUSTOM",
            [new("value", ParameterType.Number), new("array", ParameterType.Any, shape: ParameterShape.Array)],
            (args, _) => CellValue.Number(args[0].NumberValue + args[1].Rows())));

        Data(EvalArray("=CUSTOM({1;2},{10;20;30})"))
            .Should().BeEquivalentTo(new object[][] { [4.0], [5.0] });
    }
}
