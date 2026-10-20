using System;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class StructuredReferenceSuggestionTests
{
    [Test]
    [TestCase("=Sales[Am", "Sales", "Am", 7)]
    [TestCase("=Sales[[#Headers],[U", "Sales", "U", 19)]
    [TestCase("=[@", null, "", 3)]
    [TestCase("=SUM(Sales[", "Sales", "", 11)]
    [TestCase("=SUM(Sales[Amount], Sales[#Hea", "Sales", "#Hea", 26)]
    [TestCase("=Sales[[Qty]:[ Pr", "Sales", "Pr", 15)]
    [TestCase("=Sales[Col'@x", "Sales", "Col@x", 7)]
    [TestCase("=1+[", null, "", 4)]
    public void Context_Is_The_Table_And_The_Item_Being_Typed(string text, string? tableName, string prefix,
        int prefixStart)
    {
        StructuredReferenceSyntax.TryGetSuggestionContext(text, out var actualTable, out var actualPrefix,
            out var actualStart).Should().BeTrue();
        actualTable.Should().Be(tableName);
        actualPrefix.Should().Be(prefix);
        actualStart.Should().Be(prefixStart);
    }

    [Test]
    [TestCase("=\"[\" & A1")]
    [TestCase("=Sales[Amount]")]
    [TestCase("=Sales[Amount] + Sales[@Qty]")]
    [TestCase("=SUM(A1")]
    [TestCase("=Sales[[#Headers]")]
    [TestCase("")]
    public void Not_Inside_An_Item_Has_No_Context(string text)
    {
        StructuredReferenceSyntax.TryGetSuggestionContext(text, out _, out _, out _).Should().BeFalse();
    }

    [Test]
    [TestCase("=Sales[Am", 6)]
    [TestCase("=\"[\" & Sales[", 12)]
    [TestCase("=Sales[[#Headers],[U", 6)]
    [TestCase("=Sales[Amount]", -1)]
    [TestCase("=Sales[A']", 6)]
    [TestCase("='Data [West'!A1+SU", -1)]
    [TestCase("='Q[1] data'!A1+SU", -1)]
    [TestCase("='It''s [x'!A1+SU", -1)]
    [TestCase("='Data [West'!A1+Sales[Am", 22)]
    [TestCase("='Data [West", -1)]
    public void Unclosed_Bracket_Is_Found(string text, int expected)
    {
        StructuredReferenceSyntax.FindUnclosedBracket(text).Should().Be(expected);
    }

    [Test]
    [TestCase("='Data [West'!A1+SU")]
    [TestCase("='Q[1] data'!A1+SU")]
    public void Bracket_In_A_Quoted_Sheet_Name_Has_No_Context(string text)
    {
        StructuredReferenceSyntax.TryGetSuggestionContext(text, out _, out _, out _).Should().BeFalse();
    }

    [Test]
    public void Structured_Reference_After_A_Quoted_Sheet_Name_Has_Context()
    {
        StructuredReferenceSyntax.TryGetSuggestionContext("='Data [West'!A1+Sales[Am", out var table,
            out var prefix, out var prefixStart).Should().BeTrue();
        table.Should().Be("Sales");
        prefix.Should().Be("Am");
        prefixStart.Should().Be(23);
    }

    [Test]
    // the item is closed, and when it is one of a list of items, so is the reference
    [TestCase("=Sales[Am", 7, "Amount", false, "=Sales[Amount]", 14)]
    [TestCase("=SUM(Sales[[#Headers],[Am", 23, "Amount", false, "=SUM(Sales[[#Headers],[Amount]]", 31)]
    [TestCase("=Sales[[Am", 8, "Amount", false, "=Sales[[Amount]]", 16)]
    [TestCase("=Sales[@[Am", 9, "Amount", false, "=Sales[@[Amount]]", 17)]
    [TestCase("=Sales[#He", 7, "#Headers", true, "=Sales[#Headers]", 16)]
    // a specifier is usually followed by more items, so the caret goes before the outer bracket
    [TestCase("=Sales[[#He", 8, "#Headers", true, "=Sales[[#Headers]]", 17)]
    public void Completing_An_Item_Closes_Its_Brackets(string formula, int itemStart, string item,
        bool isSpecifier, string expected, int expectedCaret)
    {
        AssertCompletion(formula, itemStart, formula.Length, item, isSpecifier, expected, expectedCaret);
    }

    [Test]
    // brackets that are already there are reused rather than written again
    [TestCase("=Sales[Am]", 7, 9, "Amount", false, "=Sales[Amount]", 14)]
    [TestCase("=Sales[[#Headers],[Am]]", 19, 21, "Amount", false, "=Sales[[#Headers],[Amount]]", 26)]
    [TestCase("=Sales[[#Headers],[Am])", 19, 21, "Amount", false, "=Sales[[#Headers],[Amount]])", 27)]
    [TestCase("=Sales[[#H],[Amount]]", 8, 10, "#Headers", true, "=Sales[[#Headers],[Amount]]", 17)]
    public void Completing_An_Item_Reuses_Closing_Brackets(string formula, int itemStart, int itemEnd,
        string item, bool isSpecifier, string expected, int expectedCaret)
    {
        AssertCompletion(formula, itemStart, itemEnd, item, isSpecifier, expected, expectedCaret);
    }

    private static void AssertCompletion(string formula, int itemStart, int itemEnd, string item, bool isSpecifier,
        string expected, int expectedCaret)
    {
        var text = StructuredReferenceSyntax.CompleteItem(formula, itemStart, itemEnd, item, isSpecifier,
            out var replaceEnd, out var caretOffset);

        string.Concat(formula.AsSpan(0, itemStart), text, formula.AsSpan(replaceEnd)).Should().Be(expected);
        (itemStart + caretOffset).Should().Be(expectedCaret);
    }
}
