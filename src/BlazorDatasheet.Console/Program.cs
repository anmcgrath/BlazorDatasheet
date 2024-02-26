// See https://aka.ms/new-console-template for more information

using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Test.Formula;

Console.WriteLine("HEllo world");

var lexer = new Lexer();

// Eval("=AND(A1:A2)")
var env = new TestEnvironment();
var evaluator = new Evaluator(env);

var parser = new Parser();

var tokens = lexer.Lex($"=a:b:a2");
var st = parser.Parse(tokens);

var res = evaluator.Evaluate(st, false);

Console.WriteLine(res);