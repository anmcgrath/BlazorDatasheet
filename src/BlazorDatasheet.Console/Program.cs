// See https://aka.ms/new-console-template for more information

using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

Console.WriteLine("HEllo world");

var lexer = new Lexer();

var res = lexer.Lex($"=a:a+b1+d1:c:e");

Console.WriteLine(string.Join(",", res.Select(x => x.ToString())));