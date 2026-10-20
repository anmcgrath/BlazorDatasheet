using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatashet.Formula.Functions.Math;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class StructuredReferenceParserTests
{
    private static CellFormula Parse(string formula, string sheet = "Sheet1")
    {
        var env = new TestEnvironment();
        env.RegisterFunction(SumFunction.Descriptor);
        return new Parser(env).FromString(formula, new ParsingContext(sheet, false));
    }

    private static StructuredReference Structured(CellFormula formula) =>
        formula.References.OfType<StructuredReference>().Single();

    [TestCase("=SUM(Sales[Amount])", "=SUM(Sales[Amount])")]
    [TestCase("=Sales[@Qty]", "=Sales[@Qty]")]
    [TestCase("=Sales[[#This Row],[Qty]]", "=Sales[@Qty]")]
    [TestCase("=Sales[[#Headers],[Col1]:[Col2]]", "=Sales[[#Headers],[Col1]:[Col2]]")]
    [TestCase("=Sales[#All]", "=Sales[#All]")]
    [TestCase("=Sales[[Col1]:[Col2]]", "=Sales[[Col1]:[Col2]]")]
    [TestCase("=[Amount]", "=[Amount]")]
    [TestCase("=SUM(Sales[ Amount ])", "=SUM(Sales[Amount])")]
    [TestCase("=SUM(Sales[ Unit Price ])", "=SUM(Sales[Unit Price])")]
    [TestCase("=Sales[@ Qty ]", "=Sales[@Qty]")]
    [TestCase("=Sales[ #All ]", "=Sales[#All]")]
    [TestCase("=Sales[[ #Headers ],[ Col1 ]:[ Col2 ]]", "=Sales[[#Headers],[Col1]:[Col2]]")]
    [TestCase("=[ Amount ]", "=[Amount]")]
    public void Parses_Single_Structured_Reference(string formulaText, string expected)
    {
        var formula = Parse(formulaText);
        formula.IsValid().Should().BeTrue();
        formula.References.Should().HaveCount(1);

        var reference = Structured(formula);
        reference.Kind.Should().Be(ReferenceKind.Structured);
        reference.IsResolved.Should().BeFalse();
        reference.IsInvalid.Should().BeFalse();
        reference.Region.Should().BeOfType<EmptyRegion>();
        formula.ToFormulaString().Should().Be(expected);
    }

    [Test]
    public void Parses_Unqualified_This_Row_References()
    {
        var formula = Parse("=[@Qty]*[@Price]");
        formula.IsValid().Should().BeTrue();

        var references = formula.References.Cast<StructuredReference>().ToList();
        references.Should().HaveCount(2);
        references.Should().AllSatisfy(r =>
        {
            r.TableName.Should().BeNull();
            r.Items.Should().Equal(TableItemSpecifier.ThisRow);
        });
        references[0].ColumnStart.Should().Be("Qty");
        references[1].ColumnStart.Should().Be("Price");
        formula.ToFormulaString().Should().Be("=[@Qty]*[@Price]");
    }

    [Test]
    public void Exposes_Descriptor_Parts()
    {
        var reference = Structured(Parse("=Sales[[#Headers],[Col1]:[Col2]]"));
        reference.TableName.Should().Be("Sales");
        reference.ColumnStart.Should().Be("Col1");
        reference.ColumnEnd.Should().Be("Col2");
        reference.Items.Should().Equal(TableItemSpecifier.Headers);
        reference.ResolvedTableName.Should().BeNull();
    }

    [TestCase("=SUM(Sales[Amount])")]
    [TestCase("=[@Qty]*[@Price]")]
    [TestCase("=Sales[[#Headers],[Col1]:[Col2]]")]
    [TestCase("=Sales[#All]")]
    [TestCase("=Sales[[#Headers],[#Data],[Col1]]")]
    [TestCase("=Sales['[Odd'] Name]")]
    public void Round_Trips_Through_Formula_Text(string formulaText)
    {
        var original = Parse(formulaText);
        var reparsed = Parse(original.ToFormulaString());

        reparsed.ToFormulaString().Should().Be(original.ToFormulaString());
        var originalRefs = original.References.ToList();
        var reparsedRefs = reparsed.References.ToList();
        reparsedRefs.Should().HaveCount(originalRefs.Count);
        for (int i = 0; i < originalRefs.Count; i++)
            reparsedRefs[i].SameAs(originalRefs[i]).Should().BeTrue();
    }

    [Test]
    public void SameAs_Is_Case_Insensitive_And_Distinguishes_Columns()
    {
        var a = Structured(Parse("=Sales[Amount]"));
        a.SameAs(Structured(Parse("=sales[amount]"))).Should().BeTrue();
        a.SameAs(Structured(Parse("=Sales[Qty]"))).Should().BeFalse();
        a.SameAs(Structured(Parse("=Sales[@Amount]"))).Should().BeFalse();
        a.SameAs(Parse("=A1").References.Single()).Should().BeFalse();
    }

    [Test]
    public void Mixed_Formula_Has_Both_References()
    {
        var formula = Parse("=Sales[Amount]+A1");
        formula.References.Should().HaveCount(2);
        formula.References.Select(r => r.Kind).Should().Equal(ReferenceKind.Structured, ReferenceKind.Cell);
        formula.ToFormulaString().Should().Be("=Sales[Amount]+A1");
    }

    [Test]
    public void Unclosed_Bracket_Is_An_Invalid_Formula()
    {
        var formula = Parse("=Sales[Amount");
        formula.IsValid().Should().BeFalse();
    }

    [Test]
    public void Copy_Keeps_Descriptor_And_Region()
    {
        var reference = Structured(Parse("=Sales[Amount]"));
        reference.SetRegion(new Region(1, 5, 2, 2));

        var copy = (StructuredReference)reference.Copy();
        copy.SameAs(reference).Should().BeTrue();
        copy.IsResolved.Should().BeTrue();
        copy.Region.Should().NotBeSameAs(reference.Region);
        copy.Region.Equals(reference.Region).Should().BeTrue();
    }

    [Test]
    public void SetRegion_Stores_Region()
    {
        var reference = Structured(Parse("=Sales[Amount]"));
        reference.SetRegion(new Region(1, 5, 2, 2));
        reference.IsResolved.Should().BeTrue();
        reference.Region.Equals(new Region(1, 5, 2, 2)).Should().BeTrue();
        reference.ToAddressText().Should().Be("Sales[Amount]");
    }

    [Test]
    public void ShiftReferences_Leaves_Structured_Reference_Alone()
    {
        var formula = Parse("=Sales[Amount]+A1");
        var structured = Structured(formula);
        structured.SetRegion(new Region(1, 5, 0, 0));

        formula.ShiftReferences(2, 1, null);

        formula.ToFormulaString().Should().Be("=Sales[Amount]+B3");
        structured.Region.Equals(new Region(1, 5, 0, 0)).Should().BeTrue();
        structured.IsInvalid.Should().BeFalse();
    }

    [Test]
    public void InsertRowColIntoReferences_Leaves_Structured_Reference_Alone()
    {
        var formula = Parse("=Sales[Amount]+A3");
        var structured = Structured(formula);
        structured.SetRegion(new Region(1, 5, 0, 0));

        formula.InsertRowColIntoReferences(0, 2, Axis.Row, "Sheet1");
        formula.InsertRowColIntoReferences(0, 1, Axis.Col, "Sheet1");

        formula.ToFormulaString().Should().Be("=Sales[Amount]+B5");
        structured.Region.Equals(new Region(1, 5, 0, 0)).Should().BeTrue();
        structured.IsInvalid.Should().BeFalse();
    }

    [Test]
    public void InsertRowColIntoReferences_With_Band_Leaves_Structured_Reference_Alone()
    {
        var formula = Parse("=Sales[Amount]+A3");
        var structured = Structured(formula);
        structured.SetRegion(new Region(1, 5, 0, 0));

        formula.InsertRowColIntoReferences(0, 1, Axis.Row, "Sheet1", new Region(0, 10, 0, 10));

        formula.ToFormulaString().Should().Be("=Sales[Amount]+A4");
        structured.Region.Equals(new Region(1, 5, 0, 0)).Should().BeTrue();
    }

    [Test]
    public void RemoveRowColFromReferences_Leaves_Structured_Reference_Alone()
    {
        var formula = Parse("=Sales[Amount]+A10");
        var structured = Structured(formula);
        structured.SetRegion(new Region(1, 5, 0, 0));

        formula.RemoveRowColFromReferences(0, 2, Axis.Row, "Sheet1");

        formula.ToFormulaString().Should().Be("=Sales[Amount]+A8");
        structured.Region.Equals(new Region(1, 5, 0, 0)).Should().BeTrue();
        structured.IsInvalid.Should().BeFalse();
    }

    [Test]
    public void Removing_Rows_Containing_The_Resolved_Region_Keeps_Structured_Reference_Valid()
    {
        var formula = Parse("=Sales[@Amount]+A10");
        var structured = Structured(formula);
        structured.SetRegion(new Region(3, 3, 0, 0));

        formula.RemoveRowColFromReferences(2, 3, Axis.Row, "Sheet1");
        formula.RemoveRowColFromReferences(0, 1, Axis.Col, "Sheet1", new Region(0, 20, 0, 5));

        structured.IsInvalid.Should().BeFalse();
        structured.Region.Equals(new Region(3, 3, 0, 0)).Should().BeTrue();
        formula.ToFormulaString().Should().Be("=Sales[@Amount]+#REF!");
    }

    [Test]
    public void MoveReferencesWithin_Leaves_Structured_Reference_Alone()
    {
        var formula = Parse("=Sales[Amount]+A2");
        var structured = Structured(formula);
        structured.SetRegion(new Region(1, 5, 0, 0));

        formula.MoveReferencesWithin(new Region(0, 10, 0, 0), "Sheet1", 0, 2, "Sheet2", "Sheet1")
            .Should().BeTrue();

        formula.ToFormulaString().Should().Be("=Sales[Amount]+Sheet2!C2");
        structured.Region.Equals(new Region(1, 5, 0, 0)).Should().BeTrue();
        structured.SheetName.Should().Be("Sheet1");
        structured.IsInvalid.Should().BeFalse();
    }

    [Test]
    public void QualifyForeignReferences_Does_Not_Qualify_Structured_Reference()
    {
        var formula = Parse("=Sales[Amount]+A1", "Sheet2");
        formula.QualifyForeignReferences("Sheet1");

        Structured(formula).ExplicitSheetName.Should().BeFalse();
        formula.ToFormulaString().Should().Be("=Sales[Amount]+Sheet2!A1");
    }
}
