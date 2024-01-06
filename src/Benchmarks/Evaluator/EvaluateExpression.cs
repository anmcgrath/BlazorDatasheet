using BenchmarkDotNet.Attributes;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;
using BlazorDatasheet.Test.Formula;

namespace Benchmarks.Evaluator;

public class EvaluateExpression
{
    private BlazorDatasheet.Formula.Core.Interpreter.Syntax.SyntaxTree s1;
    private BlazorDatasheet.Formula.Core.Interpreter2.Parsing.SyntaxTree s2;
    private BlazorDatasheet.Formula.Core.FormulaEvaluator e1;
    private BlazorDatasheet.Formula.Core.Interpreter2.Evaluation.Evaluator e2;

    [GlobalSetup]
    public void Setup()
    {
        var unit = "1+(2-3)+10-100*7/2+A1";
        var expreString = "=" + string.Join("+", Enumerable.Repeat(unit, 100));
        var l1 = new BlazorDatasheet.Formula.Core.Interpreter.Syntax.Lexer();
        var l2 = new BlazorDatasheet.Formula.Core.Interpreter2.Lexing.Lexer();
        var p1 = new Parser();
        var p2 = new BlazorDatasheet.Formula.Core.Interpreter2.Parsing.Parser();

        s1 = p1.Parse(l1, expreString);
        s2 = p2.Parse(l2.Lex(expreString));

        e1 = new FormulaEvaluator(new TestEnvironment());
        e2 = new BlazorDatasheet.Formula.Core.Interpreter2.Evaluation.Evaluator(new TestEnvironment());
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