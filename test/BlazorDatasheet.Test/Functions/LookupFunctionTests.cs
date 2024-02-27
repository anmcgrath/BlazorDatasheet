using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Lookup;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Functions;

public class LookupFunctionTests
{
    private TestEnvironment _env;

    [SetUp]
    public void Setup()
    {
        _env = new();
    }

    public object? Eval(string formulaString, bool resolveReferences = false)
    {
        var eval = new Evaluator(_env);
        var parser = new Parser();
        var formula = parser.Parse(formulaString);
        return eval.Evaluate(formula, resolveReferences).Data;
    }

    [Test]
    public void VLookup_With_Range_False_Tests()
    {
        _env.RegisterFunction("VLOOKUP", new VLookupFunction());
        Eval("=VLOOKUP(2,{1;2},1,false)").Should().Be(2);
        // a lookup value outside of the array should be false
        Eval("=VLOOKUP(2,{1;3},1,false)").Should().BeOfType<FormulaError>();
        //lookup column 2
        Eval("=VLOOKUP(4,{1,3;4,5},2,false)").Should().Be(5);
    }

    [Test]
    public void VLookup_With_Range_True_Tests()
    {
        _env.RegisterFunction("VLOOKUP", new VLookupFunction());
        Eval("=VLOOKUP(3,{1;2;4},1)").Should().Be(2);
        Eval("=VLOOKUP(5,{1;2;4},1,true)").Should().Be(4);
        // a lookup value outside of the array should be false
        Eval("=VLOOKUP(0,{1;3;4},1,true)").Should().BeOfType<FormulaError>();
        //lookup column 2
        // data:
        // 1  3
        // 4  5
        Eval("=VLOOKUP(4,{1,3;4,5},2,true)").Should().Be(5);
    }
}