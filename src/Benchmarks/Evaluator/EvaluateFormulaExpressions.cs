using BenchmarkDotNet.Attributes;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;

namespace Benchmarks.Evaluator;

public class EvaluateFormulaExpressions
{
    private BlazorDatasheet.Formula.Core.Interpreter.Syntax.SyntaxTree s1;
    private BlazorDatasheet.Formula.Core.Interpreter2.Parsing.SyntaxTree s2;
    private BlazorDatasheet.Formula.Core.FormulaEvaluator e1;
    private BlazorDatasheet.Formula.Core.Interpreter2.Evaluation.Evaluator e2;
    private IEnvironment env = new TestEnvironment();

    [GlobalSetup]
    public void Setup()
    {
        var expreString = "=sum(A1:A2) + sum(1,2,3,4) / sum(a,b)";
        var l1 = new BlazorDatasheet.Formula.Core.Interpreter.Syntax.Lexer();
        var l2 = new BlazorDatasheet.Formula.Core.Interpreter2.Lexing.Lexer();
        var p1 = new BlazorDatasheet.Formula.Core.Interpreter.Syntax.Parser();
        var p2 = new BlazorDatasheet.Formula.Core.Interpreter2.Parsing.Parser();

        var testEnvironment = new TestEnvironment();
        testEnvironment.RegisterFunction("sum", new SumFunction());

        s1 = p1.Parse(l1, expreString);
        s2 = p2.Parse(l2.Lex(expreString));

        testEnvironment.SetCellValue(0, 0, 5);
        testEnvironment.SetVariable("a", 10);
        testEnvironment.SetVariable("b", 10);

        e1 = new FormulaEvaluator(testEnvironment);
        e2 = new BlazorDatasheet.Formula.Core.Interpreter2.Evaluation.Evaluator(testEnvironment);
    }

    [Benchmark]
    public void Eval1()
    {
        var result = e1.Evaluate(s1);
    }

    [Benchmark]
    public void Eval2()
    {
        var result = e2.Evaluate(s2);
    }
}