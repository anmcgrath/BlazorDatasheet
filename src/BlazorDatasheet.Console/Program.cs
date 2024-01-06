// See https://aka.ms/new-console-template for more information

using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter2.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter2.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter2.Parsing;
using BlazorDatasheet.Test.Formula;
using BlazorDatashet.Formula.Functions.Math;

var expreString = "=sum(A1:A2) + sum(1,2,3,4) / sum(a,b)";
var l1 = new BlazorDatasheet.Formula.Core.Interpreter.Syntax.Lexer();
var l2 = new BlazorDatasheet.Formula.Core.Interpreter2.Lexing.Lexer();
var p1 = new BlazorDatasheet.Formula.Core.Interpreter.Syntax.Parser();
var p2 = new BlazorDatasheet.Formula.Core.Interpreter2.Parsing.Parser();

var s1 = p1.Parse(l1, expreString);
var s2 = p2.Parse(l2.Lex(expreString));

var testEnvironment = new TestEnvironment();
testEnvironment.RegisterFunction("sum", new SumFunction());
testEnvironment.SetCellValue(0, 0, 5);
testEnvironment.SetVariable("a", 10d);
testEnvironment.SetVariable("b", 10d);

var e1 = new FormulaEvaluator(testEnvironment);
var e2 = new BlazorDatasheet.Formula.Core.Interpreter2.Evaluation.Evaluator(testEnvironment);

for (int i = 0; i < 100000; i++)
{
    var res = e1.Evaluate(s1);
}

/*while (true)
{
    var text = Console.ReadLine()!;
    var l2 = new Lexer();
    var tokens = l2.Lex(text);
    Console.WriteLine(string.Join("", tokens.Select(x => x.ToString())));

    var parser = new Parser();
    var tree = parser.Parse(tokens);
    Console.WriteLine(tree.Root.ToExpressionText());
    //var eval = new Evaluator(new TestEnvironment());
    //Console.WriteLine(eval.Evaluate(tree).Data);
}*/