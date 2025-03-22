using System.Linq;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class LexerTests
{
    [Test]
    [TestCase("=Sheet1!A1", "Sheet1")]
    [TestCase("=Sheet2!A1", "Sheet2")]
    [TestCase("='Sheet 1'!A1", "Sheet 1")]
    [TestCase("='Sheet 1'!A1:'Sheet 1'!:A1", "Sheet 1")]
    public void Sheet_Locator_Token_Tests(string formulaStr, string? expectedSheetName)
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex(formulaStr, new FormulaOptions());
        tokens = tokens.Skip(1).ToArray(); // skip = token
        tokens.First().Should().BeOfType<SheetLocatorToken>();
        tokens.First().Text.Should().BeEquivalentTo(expectedSheetName);
    }

    [Test]
    public void Bad_Sheet_Name_Should_Be_Bad_Token()
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex("='Sheet1!A1", new FormulaOptions());
        tokens.Should().NotContainEquivalentOf(new SheetLocatorToken("Sheet1", 1));
        tokens.Should().ContainEquivalentOf(new BadToken(1));
    }
}