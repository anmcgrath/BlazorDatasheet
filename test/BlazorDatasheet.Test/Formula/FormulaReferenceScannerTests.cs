using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class FormulaReferenceScannerTests
{
    private static string TextOf(string formula, FormulaReferenceSpan span) =>
        formula.Substring(span.TextStart, span.TextLength);

    [Test]
    public void Each_Reference_Gets_Its_Own_Color_Index()
    {
        var formula = "=SUM(A1:A2,A5:A5,A6:A7)";
        var spans = FormulaReferenceScanner.Scan(formula, new FormulaOptions());

        spans.Select(x => TextOf(formula, x)).Should().Equal("A1:A2", "A5:A5", "A6:A7");
        spans.Select(x => x.ColorIndex).Should().Equal(1, 2, 3);
        spans.Select(x => x.Index).Should().Equal(0, 1, 2);
    }

    [Test]
    public void Color_Indices_Cycle()
    {
        var spans = FormulaReferenceScanner.Scan("=A1+A2+A3+A4+A5+A6", new FormulaOptions());
        spans.Select(x => x.ColorIndex).Should().Equal(1, 2, 3, 4, 5, 1);
    }

    [Test]
    public void Sheet_Qualified_Range_Is_One_Reference()
    {
        var formula = "=SUM('Sheet 1'!A1:'Sheet 1'!A2,Sheet2!B2)";
        var spans = FormulaReferenceScanner.Scan(formula, new FormulaOptions());

        spans.Select(x => TextOf(formula, x)).Should().Equal("'Sheet 1'!A1:'Sheet 1'!A2", "Sheet2!B2");
        spans.Select(x => x.SheetName).Should().Equal("Sheet 1", "Sheet2");
        spans[0].Region.Should().BeEquivalentTo(new Region(0, 1, 0, 0));
        spans[1].Kind.Should().Be(FormulaReferenceSpanKind.Cell);
    }

    [Test]
    public void Spans_Exclude_Surrounding_Whitespace()
    {
        var formula = "= A1  + B2:C3 ";
        var spans = FormulaReferenceScanner.Scan(formula, new FormulaOptions());
        spans.Select(x => TextOf(formula, x)).Should().Equal("A1", "B2:C3");
    }

    [Test]
    public void Function_Name_That_Looks_Like_An_Address_Is_Not_A_Reference()
    {
        var formula = "=LOG10(A1)";
        var spans = FormulaReferenceScanner.Scan(formula, new FormulaOptions());
        spans.Select(x => TextOf(formula, x)).Should().Equal("A1");
    }

    [Test]
    public void Fixed_Flags_Are_Read()
    {
        var span = FormulaReferenceScanner.Scan("=$A1:B$2", new FormulaOptions()).Single();
        span.IsStartColFixed.Should().BeTrue();
        span.IsStartRowFixed.Should().BeFalse();
        span.IsEndColFixed.Should().BeFalse();
        span.IsEndRowFixed.Should().BeTrue();
    }

    [Test]
    public void Row_And_Column_Ranges_Are_Read()
    {
        var spans = FormulaReferenceScanner.Scan("=SUM(A:C)+SUM(2:4)", new FormulaOptions());
        spans[0].Region.Should().BeOfType<ColumnRegion>();
        spans[0].Region!.Left.Should().Be(0);
        spans[0].Region!.Right.Should().Be(2);
        spans[1].Region.Should().BeOfType<RowRegion>();
        spans[1].Region!.Top.Should().Be(1);
        spans[1].Region!.Bottom.Should().Be(3);
    }

    [Test]
    [TestCase("=SUM(A1:", "A1")]
    [TestCase("=A1+", "A1")]
    [TestCase("=SUM(A1,", "A1")]
    [TestCase("=SU", "")]
    public void Incomplete_Formula_Is_Scanned(string formula, string expected)
    {
        var spans = FormulaReferenceScanner.Scan(formula, new FormulaOptions());
        string.Join("|", spans.Select(x => TextOf(formula, x))).Should().Be(expected);
    }

    [Test]
    public void Text_That_Is_Not_A_Formula_Has_No_References()
    {
        FormulaReferenceScanner.Scan("A1", new FormulaOptions()).Should().BeEmpty();
        FormulaReferenceScanner.Scan("", new FormulaOptions()).Should().BeEmpty();
    }

    [Test]
    public void Names_Are_References_Only_When_Identified()
    {
        var formula = "=myName+A1+sum(other)";
        FormulaReferenceScanner.Scan(formula, new FormulaOptions())
            .Select(x => TextOf(formula, x)).Should().Equal("A1");

        var spans = FormulaReferenceScanner.Scan(formula, new FormulaOptions(), name => name == "myName");
        spans.Select(x => TextOf(formula, x)).Should().Equal("myName", "A1");
        spans[0].Kind.Should().Be(FormulaReferenceSpanKind.Named);
        spans[0].Name.Should().Be("myName");
        spans.Select(x => x.ColorIndex).Should().Equal(1, 2);
    }

    [Test]
    [TestCase("=A1+B2:C3")]
    [TestCase("=SUM(A:B, 2:3) + $D$4")]
    [TestCase("=Sheet1!A1 + Sheet1!B2:Sheet1!C4")]
    public void Scanned_Regions_Match_Parsed_References(string formula)
    {
        var sheet = new Sheet(10, 10);
        var parsed = sheet.FormulaEngine.ParseFormula(formula, sheet.Name).References.ToList();
        var spans = FormulaReferenceScanner.Scan(formula, sheet.FormulaEngine.Options);

        spans.Count.Should().Be(parsed.Count);
        for (int i = 0; i < parsed.Count; i++)
            spans[i].Region.Should().BeEquivalentTo(parsed[i].Region);
    }
}
