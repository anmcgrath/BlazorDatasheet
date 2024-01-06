using BenchmarkDotNet.Attributes;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace Benchmarks.Lexer;

public class LexString
{
    private string _string;

    [GlobalSetup]
    public void Setup()
    {
        var text =
            "and the true FALSE ; [,,] ())) \"asd  ss  ss\" 0.12 $A1:A3 1:2 A:B A$4:A7 333123 2213 120.123 123331 - +123 123 123---123--12";
        _string = string.Join(" ", Enumerable.Repeat(text, 200).ToString());
    }

    [Benchmark]
    public void CurrentLexer()
    {
        var lexer = new BlazorDatasheet.Formula.Core.Interpreter.Syntax.Lexer();
        SyntaxToken token;
        var tokens = new List<SyntaxToken>();
        lexer.Begin(_string);
        do
        {
            token = lexer.Lex();
            if (token.Kind != SyntaxKind.WhitespaceToken &&
                token.Kind != SyntaxKind.BadToken)
                tokens.Add(token);
        } while (token.Kind != SyntaxKind.EndOfFileToken);

        var tkens = tokens.ToArray();
    }

    [Benchmark]
    public void NewLexer()
    {
        var lexer = new BlazorDatasheet.Formula.Core.Interpreter2.Lexing.Lexer();
        var tokens = lexer.Lex(_string);
    }
}