using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class LexerTests
{
    [Test]
    [TestCase("=Sheet1!A1", "Sheet1")]
    [TestCase("=Sheet2!A1", "Sheet2")]
    [TestCase("='Sheet 1'!A1", "Sheet 1")]
    [TestCase("='Sheet''1'!A1", "Sheet'1")]
    [TestCase("='Sheet 1'!A1:'Sheet 1'!:A1", "Sheet 1")]
    public void Sheet_Locator_Token_Tests(string formulaStr, string? expectedSheetName)
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex(formulaStr, new FormulaOptions());
        tokens = tokens.Skip(1).ToList(); // skip = token
        tokens.First().Should().BeOfType<SheetLocatorToken>();
        tokens.First().Text.Should().BeEquivalentTo(expectedSheetName);
    }

    [Test]
    [TestCase("Sheet1", "Sheet1!")]
    [TestCase("Sheet_1.a", "Sheet_1.a!")]
    [TestCase("My sheet", "'My sheet'!")]
    [TestCase("Bob's", "'Bob''s'!")]
    [TestCase("2024", "'2024'!")]
    [TestCase("A-B", "'A-B'!")]
    [TestCase("true", "'true'!")]
    public void Sheet_Prefix_Is_Quoted_When_Needed_And_Is_Read_Back(string sheetName, string expectedPrefix)
    {
        var prefix = RangeText.SheetPrefix(sheetName);
        prefix.Should().Be(expectedPrefix);

        var tokens = new Lexer().Lex("=" + prefix + "A1", new FormulaOptions());
        tokens[1].Should().BeOfType<SheetLocatorToken>();
        tokens[1].Text.Should().Be(sheetName);
    }

    [Test]
    public void Bad_Sheet_Name_Should_Be_Bad_Token()
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex("='Sheet1!A1", new FormulaOptions());
        tokens.Should().NotContainEquivalentOf(new SheetLocatorToken("Sheet1", 1));
        tokens.Should().ContainEquivalentOf(new BadToken(1));
    }

    [Test]
    public void Escaped_Quotes_In_String_Are_Lexed()
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex("=\"a\"\"b\"", new FormulaOptions());
        tokens[1].Should().BeOfType<StringToken>();
        ((StringToken)tokens[1]).Value.Should().Be("a\"b");
    }

    [Test]
    [TestCase("abc1")]
    [TestCase("xfd1")]
    [TestCase("XFD1048576")]
    [TestCase("$A$1")]
    public void Text_Inside_The_Last_Row_And_Column_Is_An_Address(string text)
    {
        var tokens = new Lexer().Lex("=" + text, new FormulaOptions());
        tokens[1].Should().BeOfType<AddressToken>();
    }

    [Test]
    [TestCase("couchinmv1")]
    [TestCase("abcd1")]
    [TestCase("xfe1")]
    [TestCase("A1048577")]
    [TestCase("A0")]
    [TestCase("zzzzzzzzzzzzzzzz1")]
    public void Text_Past_The_Last_Row_Or_Column_Is_A_Name(string text)
    {
        var tokens = new Lexer().Lex("=" + text + "+1", new FormulaOptions());
        tokens[1].Should().BeOfType<IdentifierToken>();
        ((IdentifierToken)tokens[1]).Value.Should().Be(text);
    }

    [Test]
    [TestCase(0, "A")]
    [TestCase(25, "Z")]
    [TestCase(26, "AA")]
    [TestCase(16383, "XFD")]
    [TestCase(18278, "AAAA")]
    public void Column_Letters_Round_Trip(int colIndex, string letters)
    {
        RangeText.ColIndexToLetters(colIndex).Should().Be(letters);
        if (colIndex < RangeText.MaxCols)
            RangeText.ColStrToIndex(letters).Should().Be(colIndex);
        else
            RangeText.ColStrToIndex(letters).Should().BeGreaterThanOrEqualTo(RangeText.MaxCols);
    }
}
