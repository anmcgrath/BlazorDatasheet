using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class StructuredReferenceLexerTests
{
    private const TableItemSpecifier All = TableItemSpecifier.All;
    private const TableItemSpecifier Data = TableItemSpecifier.Data;
    private const TableItemSpecifier Headers = TableItemSpecifier.Headers;
    private const TableItemSpecifier Totals = TableItemSpecifier.Totals;
    private const TableItemSpecifier ThisRow = TableItemSpecifier.ThisRow;

    private static StructuredReferenceDescriptor D(string? table, string? start = null, string? end = null,
        params TableItemSpecifier[] items) => new(table, items, start, end);

    private static IEnumerable<TestCaseData> SyntaxForms()
    {
        yield return new TestCaseData("Sales[Amount]", D("Sales", "Amount"));
        yield return new TestCaseData("Sales[Unit Price]", D("Sales", "Unit Price"));
        yield return new TestCaseData("Sales[@Amount]", D("Sales", "Amount", null, ThisRow));
        yield return new TestCaseData("Sales[@]", D("Sales", null, null, ThisRow));
        yield return new TestCaseData("Sales[]", D("Sales"));
        yield return new TestCaseData("Sales[#All]", D("Sales", null, null, All));
        yield return new TestCaseData("Sales[#Data]", D("Sales", null, null, Data));
        yield return new TestCaseData("Sales[#Headers]", D("Sales", null, null, Headers));
        yield return new TestCaseData("Sales[#headers]", D("Sales", null, null, Headers));
        yield return new TestCaseData("Sales[#Totals]", D("Sales", null, null, Totals));
        yield return new TestCaseData("Sales[#This Row]", D("Sales", null, null, ThisRow));
        yield return new TestCaseData("Sales['[Weird]", D("Sales", "[Weird"));
        yield return new TestCaseData("Sales[@[Unit Price]]", D("Sales", "Unit Price", null, ThisRow));
        yield return new TestCaseData("Sales[@[C1]:[C2]]", D("Sales", "C1", "C2", ThisRow));
        yield return new TestCaseData("Sales[[#Headers],[Col]]", D("Sales", "Col", null, Headers));
        yield return new TestCaseData("Sales[[#This Row],[Col]]", D("Sales", "Col", null, ThisRow));
        yield return new TestCaseData("Sales[[C1]:[C2]]", D("Sales", "C1", "C2"));
        yield return new TestCaseData("Sales[[#Data],[C1]:[C2]]", D("Sales", "C1", "C2", Data));
        yield return new TestCaseData("Sales[[#Headers],[#Data]]", D("Sales", null, null, Headers, Data));
        yield return new TestCaseData("Sales[[#Data],[#Headers]]", D("Sales", null, null, Headers, Data));
        yield return new TestCaseData("Sales[[#Data],[#Totals]]", D("Sales", null, null, Data, Totals));
        yield return new TestCaseData("Sales[[#Headers];[Col]]", D("Sales", "Col", null, Headers));
        yield return new TestCaseData("[Col]", D(null, "Col"));
        yield return new TestCaseData("[@Col]", D(null, "Col", null, ThisRow));
        yield return new TestCaseData("[[#Headers],[Col]]", D(null, "Col", null, Headers));
        yield return new TestCaseData("LOG10[x]", D("LOG10", "x"));
        yield return new TestCaseData("A1[x]", D("A1", "x"));
        // whitespace
        yield return new TestCaseData("Sales[ Amount ]", D("Sales", "Amount"));
        yield return new TestCaseData("Sales[Unit Price]", D("Sales", "Unit Price"));
        yield return new TestCaseData("Sales[ Unit Price ]", D("Sales", "Unit Price"));
        yield return new TestCaseData("Sales[@ Amount]", D("Sales", "Amount", null, ThisRow));
        yield return new TestCaseData("Sales[@ Amount ]", D("Sales", "Amount", null, ThisRow));
        yield return new TestCaseData("Sales[@ ]", D("Sales", null, null, ThisRow));
        yield return new TestCaseData("Sales[ #Headers ]", D("Sales", null, null, Headers));
        yield return new TestCaseData("Sales[ [#Headers] , [ Amount ] ]", D("Sales", "Amount", null, Headers));
        yield return new TestCaseData("Sales[[ #Data ],[ Amount ]]", D("Sales", "Amount", null, Data));
        yield return new TestCaseData("Sales[[ Qty ]:[ Amount ]]", D("Sales", "Qty", "Amount"));
        yield return new TestCaseData("Sales[@[ Amount ]]", D("Sales", "Amount", null, ThisRow));
        yield return new TestCaseData("Sales[ '#Col ]", D("Sales", "#Col"));
        yield return new TestCaseData("Sales[[C1] : [C2]]", D("Sales", "C1", "C2"));
        yield return new TestCaseData("Sales[@ [C1]]", D("Sales", "C1", null, ThisRow));
        // escapes
        yield return new TestCaseData("Sales['#Col]", D("Sales", "#Col"));
        yield return new TestCaseData("Sales['@x]", D("Sales", "@x"));
        yield return new TestCaseData("Sales[a''b]", D("Sales", "a'b"));
        yield return new TestCaseData("Sales[[a']b]]", D("Sales", "a]b"));
    }

    private static List<Token> Lex(string formula, out List<string> errors,
        WhiteSpaceOptions whiteSpaceOptions = WhiteSpaceOptions.RemoveWhitespace)
    {
        var lexer = new Lexer();
        var tokens = lexer.Lex(formula, new FormulaOptions(), whiteSpaceOptions);
        errors = lexer.Errors;
        return tokens;
    }

    [Test]
    [TestCaseSource(nameof(SyntaxForms))]
    public void Syntax_Form_Lexes_To_Single_Structured_Reference(string text, StructuredReferenceDescriptor expected)
    {
        var tokens = Lex("=" + text, out var errors);
        errors.Should().BeEmpty();
        tokens.Select(t => t.Tag).Should()
            .Equal(Tag.EqualsToken, Tag.StructuredReferenceToken, Tag.Eof);

        var token = (StructuredReferenceToken)tokens[1];
        token.Text.Should().Be(text);
        token.PositionStart.Should().Be(1);
        token.Descriptor.Should().Be(expected);
    }

    [Test]
    [TestCaseSource(nameof(SyntaxForms))]
    public void Format_And_Parse_Round_Trip(string text, StructuredReferenceDescriptor expected)
    {
        var formatted = StructuredReferenceSyntax.Format(expected);
        var tokens = Lex("=" + formatted, out var errors);
        errors.Should().BeEmpty();
        var token = (StructuredReferenceToken)tokens[1];
        token.Text.Should().Be(formatted);
        token.Descriptor.Should().Be(expected);

        StructuredReferenceSyntax.Format(token.Descriptor).Should().Be(formatted);
    }

    [Test]
    [TestCase("Sales[Amount]", "Sales[Amount]")]
    [TestCase("Sales[ Amount ]", "Sales[Amount]")]
    [TestCase("Sales[[ #Data ],[ Qty ]:[ Amount ]]", "Sales[[#Data],[Qty]:[Amount]]")]
    [TestCase("Sales[ #Headers ]", "Sales[#Headers]")]
    [TestCase("Sales[@[Amount]]", "Sales[@Amount]")]
    [TestCase("Sales[[#This Row],[Amount]]", "Sales[@Amount]")]
    [TestCase("Sales[#This Row]", "Sales[@]")]
    [TestCase("Sales[[#headers]]", "Sales[#Headers]")]
    [TestCase("Sales[[#Data],[#Headers]]", "Sales[[#Headers],[#Data]]")]
    [TestCase("Sales[[#Totals],[#Data],[A]:[B]]", "Sales[[#Data],[#Totals],[A]:[B]]")]
    [TestCase("Sales[[#Headers],[Col]]", "Sales[[#Headers],[Col]]")]
    [TestCase("Sales[[C1]:[C2]]", "Sales[[C1]:[C2]]")]
    [TestCase("[@C1]", "[@C1]")]
    [TestCase("Sales['#Col]", "Sales['#Col]")]
    [TestCase("Sales[[a']b]]", "Sales[a']b]")]
    public void Format_Is_Canonical(string text, string expected)
    {
        var tokens = Lex("=" + text, out _);
        var token = (StructuredReferenceToken)tokens[1];
        StructuredReferenceSyntax.Format(token.Descriptor).Should().Be(expected);
        token.Descriptor.ToString().Should().Be(expected);
    }

    [Test]
    public void Structured_Reference_Does_Not_Swallow_Neighbours()
    {
        var tokens = Lex("=Sales[Amount]+1", out var errors);
        errors.Should().BeEmpty();
        tokens.Select(t => t.Tag).Should().Equal(Tag.EqualsToken, Tag.StructuredReferenceToken, Tag.PlusToken,
            Tag.Number, Tag.Eof);
        tokens[2].PositionStart.Should().Be(14);
    }

    [Test]
    public void Structured_References_In_Function_Call()
    {
        var tokens = Lex("=SUM(Sales[Amount],[@Qty]*2)", out var errors);
        errors.Should().BeEmpty();
        tokens.Select(t => t.Tag).Should().Equal(Tag.EqualsToken, Tag.IdentifierToken, Tag.LeftParenthToken,
            Tag.StructuredReferenceToken, Tag.CommaToken, Tag.StructuredReferenceToken, Tag.StarToken,
            Tag.Number, Tag.RightParenthToken, Tag.Eof);
        tokens[5].Text.Should().Be("[@Qty]");
    }

    [Test]
    public void Error_And_Spill_Tokens_Are_Unchanged()
    {
        var tokens = Lex("=#REF!", out var errors);
        errors.Should().BeEmpty();
        tokens[1].Should().BeOfType<ErrorToken>();

        tokens = Lex("=A1#", out errors);
        errors.Should().BeEmpty();
        tokens[1].Should().BeOfType<AddressToken>().Which.IsSpillRange.Should().BeTrue();

        tokens = Lex("=LOG10(1)", out errors);
        tokens[1].Should().BeOfType<IdentifierToken>();
    }

    [Test]
    public void Name_Then_Space_Then_Bracket_Is_Not_Qualified()
    {
        var tokens = Lex("=Sales [Amount]", out var errors, WhiteSpaceOptions.PreserveAll);
        errors.Should().BeEmpty();
        tokens.Select(t => t.Tag).Should().Equal(Tag.EqualsToken, Tag.IdentifierToken, Tag.Whitespace,
            Tag.StructuredReferenceToken, Tag.Eof);
        ((StructuredReferenceToken)tokens[3]).Descriptor.Should().Be(D(null, "Amount"));
    }

    [Test]
    [TestCase("=Sales[Amount")]
    [TestCase("=Sales[[#Headers],[Col]")]
    [TestCase("=[")]
    [TestCase("=Sales[Col']")]
    [TestCase("=Sales[#Foo]")]
    [TestCase("=Sales[[#Headers],[#Totals]]")]
    [TestCase("=Sales[[#All],[#Data]]")]
    [TestCase("=Sales[[#This Row],[#Data]]")]
    [TestCase("=Sales[[C1],[C2]]")]
    [TestCase("=Sales[[C1]:[C2]:[C3]]")]
    [TestCase("=Sales[[Col],[#Headers]]")]
    [TestCase("=Sales[[#Headers],[#Headers]]")]
    [TestCase("=Sales[@[#Headers]]")]
    [TestCase("=Sales[[#Headers]:[Col]]")]
    [TestCase("=Sales[[]]")]
    [TestCase("=Sales[[Col],]")]
    [TestCase("=Sales[[Col]x]")]
    [TestCase("=Sales[[a[b]]]")]
    [TestCase("=$Sales[Col]")]
    public void Invalid_Structured_Reference_Is_Bad_Token_With_Error(string formula)
    {
        var tokens = Lex(formula, out var errors);
        errors.Should().NotBeEmpty();
        tokens.Should().ContainSingle(t => t.Tag == Tag.BadToken);
        tokens.Should().NotContain(t => t.Tag == Tag.StructuredReferenceToken);
        tokens.Last().Tag.Should().Be(Tag.Eof);
    }

    [Test]
    public void Tokens_After_Invalid_Structured_Reference_Are_Still_Read()
    {
        var tokens = Lex("=Sales[#Foo]+1", out var errors);
        errors.Should().HaveCount(1);
        tokens.Select(t => t.Tag).Should().Equal(Tag.EqualsToken, Tag.BadToken, Tag.PlusToken, Tag.Number,
            Tag.Eof);
    }

    [Test]
    [TestCase("Amount", "Amount")]
    [TestCase("#Col", "'#Col")]
    [TestCase("@x", "'@x")]
    [TestCase("a'b", "a''b")]
    [TestCase("[a]", "'[a']")]
    [TestCase("Unit Price", "Unit Price")]
    public void Escape_And_Unescape_Column_Names(string name, string escaped)
    {
        StructuredReferenceSyntax.EscapeColumnName(name).Should().Be(escaped);
        StructuredReferenceSyntax.UnescapeColumnName(escaped).Should().Be(name);
    }

    [Test]
    public void TryParseBody_Reports_Errors()
    {
        StructuredReferenceSyntax.TryParseBody("[#Nope]", "Sales", out var descriptor, out var error)
            .Should().BeFalse();
        descriptor.Should().BeNull();
        error.Should().NotBeNullOrEmpty();

        StructuredReferenceSyntax.TryParseBody("[#Data],[A]:[B]", "Sales", out descriptor, out error)
            .Should().BeTrue();
        error.Should().BeNull();
        descriptor.Should().Be(D("Sales", "A", "B", Data));
    }

    [Test]
    public void Descriptor_Equality_Ignores_Case_And_Item_Order()
    {
        var a = D("Sales", "Amount", null, Headers, Data);
        var b = D("SALES", "amount", null, Data, Headers);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());

        a.Should().NotBe(D("Sales", "Amount", null, Headers));
        a.Should().NotBe(D("Sales", "Price", null, Headers, Data));
        a.Should().NotBe(D(null, "Amount", null, Headers, Data));
    }

    [Test]
    public void Descriptor_Helpers()
    {
        var d = D("Sales", "Amount", null, ThisRow);
        d.IsThisRow.Should().BeTrue();
        d.HasColumns.Should().BeTrue();
        D("Sales").HasColumns.Should().BeFalse();
        D("Sales").IsThisRow.Should().BeFalse();

        d.WithTableName("Orders").Should().Be(D("Orders", "Amount", null, ThisRow));
        d.WithColumnNames("A", "B").Should().Be(D("Sales", "A", "B", ThisRow));
    }

    [Test]
    [TestCase("=Sales[Am", 6)]
    [TestCase("=Sales[[#Headers],[U", 6)]
    [TestCase("=Sales[[#Headers],[Unit]", 6)]
    [TestCase("=[@", 1)]
    [TestCase("=Sales[Amount]", -1)]
    [TestCase("=Sales[Amount]+Orders[", 21)]
    [TestCase("=\"[\"&1", -1)]
    [TestCase("=\"a\"\"[\"&Sales[x", 13)]
    [TestCase("=Sales['[x", 6)]
    [TestCase("=Sales[']", 6)]
    [TestCase("=1]", -1)]
    [TestCase("", -1)]
    public void Find_Unclosed_Bracket(string text, int expected)
    {
        StructuredReferenceSyntax.FindUnclosedBracket(text).Should().Be(expected);
    }
}
