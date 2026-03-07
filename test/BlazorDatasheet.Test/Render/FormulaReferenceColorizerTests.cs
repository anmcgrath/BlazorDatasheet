using System.Linq;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class FormulaReferenceColorizerTests
{
    [Test]
    public void Range_Reference_Tokens_Use_One_Color_Index()
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex("=SUM(A1:A2,A5:A5,A6:A7)", new FormulaOptions());

        var colorIndices = FormulaReferenceColorizer.GetReferenceColorIndices(tokens);
        var visibleTokens = tokens.Take(tokens.Count - 1).ToList();
        var orderedColorIndices = colorIndices.OrderBy(x => x.Key).Select(x => x.Value).ToList();

        orderedColorIndices.Should().Equal(1, 1, 1, 2, 2, 2, 3, 3, 3);

        visibleTokens.Where((_, index) => colorIndices.ContainsKey(index))
            .Select(token => token.Tag)
            .Should()
            .OnlyContain(tag => tag == Tag.AddressToken || tag == Tag.ColonToken);
    }

    [Test]
    public void Sheet_Qualified_Range_Tokens_Share_One_Color_Index()
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex("=SUM('Sheet 1'!A1:'Sheet 1'!A2,Sheet2!B2)", new FormulaOptions());

        var colorIndices = FormulaReferenceColorizer.GetReferenceColorIndices(tokens);
        var orderedColorIndices = colorIndices.OrderBy(x => x.Key).Select(x => x.Value).ToList();
        var coloredTags = tokens.Take(tokens.Count - 1)
            .Where((_, index) => colorIndices.ContainsKey(index))
            .Select(token => token.Tag)
            .ToList();

        orderedColorIndices.Should().Equal(1, 1, 1, 1, 1, 2, 2);
        coloredTags.Should().Equal(
            Tag.SheetLocatorToken,
            Tag.AddressToken,
            Tag.ColonToken,
            Tag.SheetLocatorToken,
            Tag.AddressToken,
            Tag.SheetLocatorToken,
            Tag.AddressToken);
    }
}
