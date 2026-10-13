using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class CellFormulaMoveReferencesTests
{
    private static CellFormula Parse(string formula, string sheet = "Sheet1")
    {
        return new Parser(new TestEnvironment()).FromString(formula, new ParsingContext(sheet, false));
    }

    [Test]
    public void Reference_Fully_Inside_Region_Moves()
    {
        var f = Parse("=A1+A2");
        f.MoveReferencesWithin(new Region(0, 1, 0, 0), "Sheet1", 0, 4, "Sheet1", "Sheet1").Should().BeTrue();
        f.ToFormulaString().Should().Be("=E1+E2");
    }

    [Test]
    public void Fixed_Reference_Inside_Region_Moves_Too()
    {
        var f = Parse("=$A$1+sum($A$1:$A$2)");
        f.MoveReferencesWithin(new Region(0, 1, 0, 0), "Sheet1", 2, 1, "Sheet1", "Sheet1");
        f.ToFormulaString().Should().Be("=$B$3+sum($B$3:$B$4)");
    }

    [Test]
    public void Partially_Overlapping_Range_Is_Unchanged()
    {
        var f = Parse("=sum(A1:A3)");
        f.MoveReferencesWithin(new Region(0, 1, 0, 0), "Sheet1", 0, 4, "Sheet1", "Sheet1").Should().BeFalse();
        f.ToFormulaString().Should().Be("=sum(A1:A3)");
    }

    [Test]
    public void Reference_Outside_Region_Is_Unchanged()
    {
        var f = Parse("=B1");
        f.MoveReferencesWithin(new Region(0, 1, 0, 0), "Sheet1", 0, 4, "Sheet1", "Sheet1").Should().BeFalse();
        f.ToFormulaString().Should().Be("=B1");
    }

    [Test]
    public void Reference_To_Other_Sheet_Is_Unchanged()
    {
        var f = Parse("=Sheet2!A1");
        f.MoveReferencesWithin(new Region(0, 1, 0, 0), "Sheet1", 0, 4, "Sheet1", "Sheet1").Should().BeFalse();
        f.ToFormulaString().Should().Be("=Sheet2!A1");
    }

    [Test]
    public void Move_To_Other_Sheet_Makes_Reference_Explicit()
    {
        var f = Parse("=A1");
        f.MoveReferencesWithin(new Region(0, 0, 0, 0), "Sheet1", 1, 1, "Sheet2", "Sheet1");
        f.ToFormulaString().Should().Be("=Sheet2!B2");
        f.References.First().SheetName.Should().Be("Sheet2");
    }

    [Test]
    public void Move_To_Formula_Sheet_Keeps_Reference_Implicit()
    {
        // formula lives on Sheet2 and referenced Sheet1!A1, which moves onto Sheet2
        var f = Parse("=Sheet1!A1", "Sheet2");
        f.MoveReferencesWithin(new Region(0, 0, 0, 0), "Sheet1", 0, 0, "Sheet2", "Sheet2");
        // was explicit, stays explicit but now points at Sheet2
        f.ToFormulaString().Should().Be("=Sheet2!A1");
    }

    [Test]
    public void Named_References_Are_Unchanged()
    {
        var f = Parse("=myName");
        f.MoveReferencesWithin(new Region(0, 10, 0, 10), "Sheet1", 1, 1, "Sheet1", "Sheet1").Should().BeFalse();
        f.ToFormulaString().Should().Be("=myName");
    }
}
