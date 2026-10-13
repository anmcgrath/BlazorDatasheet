using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Functions.Arrays;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Functions;

public class ArrayFunctionTests
{
    private TestEnvironment _env;
    private Evaluator _evaluator;
    private Parser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _env.RegisterFunction(SequenceFunction.Descriptor);
        _env.RegisterFunction(TransposeFunction.Descriptor);
        _env.RegisterFunction(UniqueFunction.Descriptor);
        _env.RegisterFunction(SortFunction.Descriptor);
        _env.RegisterFunction(SortByFunction.Descriptor);
        _env.RegisterFunction(FilterFunction.Descriptor);
        _env.RegisterFunction(SumFunction.Descriptor);
        _evaluator = new Evaluator(_env);
        _parser = new Parser(_env);

        // A1:B4 - names and scores
        _env.SetCellValue(0, 0, "b");
        _env.SetCellValue(0, 1, 2);
        _env.SetCellValue(1, 0, "a");
        _env.SetCellValue(1, 1, 3);
        _env.SetCellValue(2, 0, "c");
        _env.SetCellValue(2, 1, 1);
        _env.SetCellValue(3, 0, "a");
        _env.SetCellValue(3, 1, 3);
    }

    private CellValue Eval(string formula) => _evaluator.Evaluate(_parser.FromString(formula));

    private object?[][] EvalData(string formula)
    {
        var result = Eval(formula);
        result.IsArray().Should().BeTrue($"{formula} should give an array, not {result}");
        return ((CellValue[][])result.Data!).Select(r => r.Select(c => c.Data).ToArray()).ToArray();
    }

    private ErrorType ErrorOf(string formula)
    {
        var result = Eval(formula);
        result.IsError().Should().BeTrue($"{formula} should be an error");
        return ((FormulaError)result.Data!).ErrorType;
    }

    [Test]
    public void Sequence_Fills_Row_By_Row()
    {
        EvalData("=SEQUENCE(3)").Should().BeEquivalentTo(new object[][] { [1.0], [2.0], [3.0] });
        EvalData("=SEQUENCE(2,3,10,5)").Should().BeEquivalentTo(new object[][] { [10.0, 15.0, 20.0], [25.0, 30.0, 35.0] });
        Eval("=SUM(SEQUENCE(4))").Data.Should().Be(10.0);
        ErrorOf("=SEQUENCE(0)").Should().Be(ErrorType.Value);
        ErrorOf("=SEQUENCE(100000,100000)").Should().Be(ErrorType.Num);
    }

    [Test]
    public void Transpose_Swaps_Rows_And_Columns()
    {
        EvalData("=TRANSPOSE({1,2,3;4,5,6})").Should().BeEquivalentTo(new object[][] { [1.0, 4.0], [2.0, 5.0], [3.0, 6.0] });
        EvalData("=TRANSPOSE(A1:A4)").Should().BeEquivalentTo(new object[][] { ["b", "a", "c", "a"] });
    }

    [Test]
    public void Unique_Keeps_First_Occurrences()
    {
        EvalData("=UNIQUE({1;2;2;3})").Should().BeEquivalentTo(new object[][] { [1.0], [2.0], [3.0] });
        EvalData("=UNIQUE(A1:B4)").Should().BeEquivalentTo(new object[][] { ["b", 2.0], ["a", 3.0], ["c", 1.0] });
        EvalData("=UNIQUE(A1:A4,FALSE,TRUE)").Should().BeEquivalentTo(new object[][] { ["b"], ["c"] });
        EvalData("=UNIQUE({1,2,2,3},TRUE)").Should().BeEquivalentTo(new object[][] { [1.0, 2.0, 3.0] });
    }

    [Test]
    public void Sort_Orders_Rows_By_A_Column()
    {
        EvalData("=SORT(A1:B4)").Should().BeEquivalentTo(new object[][] { ["a", 3.0], ["a", 3.0], ["b", 2.0], ["c", 1.0] });
        EvalData("=SORT(A1:B4,2,-1)").Should().BeEquivalentTo(new object[][] { ["a", 3.0], ["a", 3.0], ["b", 2.0], ["c", 1.0] });
        EvalData("=SORT(B1:B4,1,-1)").Should().BeEquivalentTo(new object[][] { [3.0], [3.0], [2.0], [1.0] });
        EvalData("=SORT({3,1,2},1,1,TRUE)").Should().BeEquivalentTo(new object[][] { [1.0, 2.0, 3.0] });
        ErrorOf("=SORT(A1:B4,3)").Should().Be(ErrorType.Value);
        ErrorOf("=SORT(A1:B4,1,2)").Should().Be(ErrorType.Value);
    }

    [Test]
    public void Sort_Is_Stable()
    {
        // the two "a" rows keep their original order (row 2 before row 4)
        _env.SetCellValue(3, 1, 4);
        EvalData("=SORT(A1:B4)").Should().BeEquivalentTo(new object[][] { ["a", 3.0], ["a", 4.0], ["b", 2.0], ["c", 1.0] });
    }

    [Test]
    public void SortBy_Orders_By_Other_Arrays()
    {
        EvalData("=SORTBY(A1:A4,B1:B4)").Should().BeEquivalentTo(new object[][] { ["c"], ["b"], ["a"], ["a"] });
        EvalData("=SORTBY(A1:A4,B1:B4,-1)").Should().BeEquivalentTo(new object[][] { ["a"], ["a"], ["b"], ["c"] });
        // ties on the score are broken by the name
        _env.SetCellValue(0, 1, 3);
        EvalData("=SORTBY(A1:A4,B1:B4,-1,A1:A4,1)").Should().BeEquivalentTo(new object[][] { ["a"], ["a"], ["b"], ["c"] });
        EvalData("=SORTBY({1,2,3},{3,1,2})").Should().BeEquivalentTo(new object[][] { [2.0, 3.0, 1.0] });
        ErrorOf("=SORTBY(A1:A4,B1:B2)").Should().Be(ErrorType.Value);
    }

    [Test]
    public void Filter_Keeps_Matching_Rows_Or_Columns()
    {
        EvalData("=FILTER(A1:B4,B1:B4>2)").Should().BeEquivalentTo(new object[][] { ["a", 3.0], ["a", 3.0] });
        EvalData("=FILTER({1,2,3;4,5,6},{1,0,1})").Should().BeEquivalentTo(new object[][] { [1.0, 3.0], [4.0, 6.0] });
        Eval("=FILTER(A1:B4,B1:B4>10,\"none\")").Data.Should().Be("none");
        Eval("=FILTER(A1:B4,B1:B4>10)").Rows().Should().Be(0);
        ErrorOf("=FILTER(A1:B4,B1:B2>0)").Should().Be(ErrorType.Value);
        ErrorOf("=FILTER(A1:B4,A1:A4)").Should().Be(ErrorType.Value);
    }
}
