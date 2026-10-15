using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class DependencyManagerCellShiftTests
{
    private DependencyManager _dm;

    [SetUp]
    public void Setup_Tests()
    {
        _dm = new DependencyManager();
        _dm.AddSheet("Sheet1");
        _dm.AddSheet("Sheet2");
    }

    private static CellFormula GetFormula(string formulaStr, string sheet = "Sheet1")
    {
        return new Parser(new TestEnvironment()).FromString(formulaStr, new ParsingContext(sheet, false));
    }

    private void Set(int row, int col, string formula, string sheet = "Sheet1") =>
        _dm.SetFormula(row, col, sheet, GetFormula(formula, sheet));

    private string FormulaAt(int row, int col, string sheet = "Sheet1") =>
        _dm.GetVertex(row, col, sheet)!.Formula!.ToFormulaString();

    /// <summary>
    /// Whether the calculation order has the formula at (depRow, depCol) depending on the formula at (precRow, precCol).
    /// </summary>
    private bool HasEdge(int precRow, int precCol, int depRow, int depCol)
    {
        return _dm.GetDependencies()
            .Where(d => d.Type == DependencyType.CalculationOrder)
            .Any(d => d.From.Equals(new Region(depRow, depCol)) && d.To.Equals(new Region(precRow, precCol)));
    }

    [Test]
    public void Insert_Cells_Adjusts_Dependents_Within_Band_Only()
    {
        Set(9, 0, "=B2+sum(A2:C2)+sum(A1:C5)+D4"); // A10
        _dm.InsertCellsAt(new Region(1, 2, 1, 1), Axis.Col, "Sheet1"); // B2:B3 shift right

        FormulaAt(9, 0).Should().Be("=C2+sum(A2:D2)+sum(A1:C5)+D4");
        _dm.HasDependents(new Region(1, 3), "Sheet1").Should().BeTrue(); // D2, by A2:D2
        _dm.HasDependents(new Region(5, 5, 0, 5), "Sheet1").Should().BeFalse(); // row 6 untracked
        _dm.HasDependents(new Region(3, 3), "Sheet1").Should().BeTrue(); // D4 still tracked
    }

    [Test]
    public void Insert_Cells_Adjusts_Dependents_On_Other_Sheets()
    {
        Set(0, 0, "=Sheet1!D2+sum(Sheet1!A1:C5)", "Sheet2");
        _dm.InsertCellsAt(new Region(1, 2, 1, 1), Axis.Col, "Sheet1");

        FormulaAt(0, 0, "Sheet2").Should().Be("=Sheet1!E2+sum(Sheet1!A1:C5)");
        _dm.HasDependents(new Region(1, 3), "Sheet1").Should().BeFalse(); // D2
        _dm.HasDependents(new Region(1, 4), "Sheet1").Should().BeTrue(); // E2
    }

    [Test]
    public void Remove_Cells_Adjusts_Dependents_On_Other_Sheets()
    {
        Set(0, 0, "=Sheet1!B2+Sheet1!D2+sum(Sheet1!A1:C5)", "Sheet2");
        _dm.RemoveCellsAt(new Region(1, 2, 1, 1), Axis.Col, "Sheet1");

        FormulaAt(0, 0, "Sheet2").Should().Be("=#REF!+Sheet1!C2+sum(Sheet1!A1:C5)");
        _dm.HasDependents(new Region(1, 3), "Sheet1").Should().BeFalse(); // D2
        _dm.HasDependents(new Region(1, 2), "Sheet1").Should().BeTrue(); // C2
    }

    [Test]
    public void Insert_Cells_Shifts_Vertices_In_Half_Band_Only()
    {
        Set(1, 0, "=1"); // A2
        Set(1, 2, "=2"); // C2
        Set(2, 1, "=3"); // B3
        Set(3, 2, "=4"); // C4 - below the band

        _dm.InsertCellsAt(new Region(1, 2, 1, 1), Axis.Col, "Sheet1");

        _dm.GetVertex(1, 0, "Sheet1").Should().NotBeNull();
        _dm.GetVertex(1, 2, "Sheet1").Should().BeNull();
        _dm.GetVertex(1, 3, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=2");
        _dm.GetVertex(2, 1, "Sheet1").Should().BeNull();
        _dm.GetVertex(2, 2, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=3");
        _dm.GetVertex(3, 2, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=4");
    }

    [Test]
    public void Insert_Cells_Shifts_Adjacent_Vertices_Without_Losing_Any()
    {
        Set(1, 1, "=1"); // B2
        Set(1, 2, "=2"); // C2
        Set(1, 3, "=3"); // D2

        _dm.InsertCellsAt(new Region(1, 1, 1, 1), Axis.Col, "Sheet1");

        _dm.GetVertex(1, 1, "Sheet1").Should().BeNull();
        FormulaAt(1, 2).Should().Be("=1");
        FormulaAt(1, 3).Should().Be("=2");
        FormulaAt(1, 4).Should().Be("=3");
    }

    [Test]
    public void Remove_Cells_Clears_Formulas_In_Region_And_Shifts_Vertices()
    {
        Set(1, 1, "=1"); // B2
        Set(1, 2, "=2"); // C2
        Set(0, 2, "=3"); // C1 - above the band
        Set(9, 0, "=B2+C2"); // A10

        _dm.RemoveCellsAt(new Region(1, 1, 1, 1), Axis.Col, "Sheet1");

        FormulaAt(1, 1).Should().Be("=2");
        _dm.GetVertex(1, 2, "Sheet1").Should().BeNull();
        FormulaAt(0, 2).Should().Be("=3");
        FormulaAt(9, 0).Should().Be("=#REF!+B2");
        HasEdge(1, 1, 9, 0).Should().BeTrue();
    }

    [Test]
    public void Remove_Cells_Shift_Up_Shifts_Vertices_And_References()
    {
        Set(2, 1, "=1"); // B3
        Set(2, 3, "=2"); // D3 - outside the band
        Set(9, 0, "=B3+sum(B1:C4)+D3"); // A10

        _dm.RemoveCellsAt(new Region(1, 1, 1, 2), Axis.Row, "Sheet1"); // B2:C2 shift up

        FormulaAt(1, 1).Should().Be("=1");
        FormulaAt(2, 3).Should().Be("=2");
        FormulaAt(9, 0).Should().Be("=B2+sum(B1:C3)+D3");
        HasEdge(1, 1, 9, 0).Should().BeTrue();
        HasEdge(2, 3, 9, 0).Should().BeTrue();
    }

    [Test]
    public void Vertex_Moving_Into_And_Out_Of_Unadjusted_Range_Is_Relinked()
    {
        Set(9, 0, "=sum(B1:D5)"); // A10 - straddles the band, so is not adjusted
        Set(1, 0, "=1"); // A2 - moves into B1:D5
        Set(1, 3, "=2"); // D2 - moves out of B1:D5

        HasEdge(1, 0, 9, 0).Should().BeFalse();
        HasEdge(1, 3, 9, 0).Should().BeTrue();

        var restore = _dm.InsertCellsAt(new Region(1, 1, 0, 0), Axis.Col, "Sheet1"); // A2 shift right

        FormulaAt(9, 0).Should().Be("=sum(B1:D5)");
        HasEdge(1, 1, 9, 0).Should().BeTrue(); // B2
        HasEdge(1, 4, 9, 0).Should().BeFalse(); // E2

        _dm.Restore(restore);

        _dm.GetVertex(1, 0, "Sheet1").Should().NotBeNull();
        _dm.GetVertex(1, 3, "Sheet1").Should().NotBeNull();
        HasEdge(1, 0, 9, 0).Should().BeFalse();
        HasEdge(1, 3, 9, 0).Should().BeTrue();
    }

    [Test]
    public void Vertex_Moving_Into_Unadjusted_Range_On_Remove_Is_Linked()
    {
        Set(9, 0, "=sum(B1:D5)"); // A10
        Set(1, 4, "=1"); // E2 - moves to D2

        HasEdge(1, 4, 9, 0).Should().BeFalse();
        _dm.RemoveCellsAt(new Region(1, 1, 1, 1), Axis.Col, "Sheet1");
        HasEdge(1, 3, 9, 0).Should().BeTrue();
        _dm.GetCalculationOrder().SelectMany(x => x).Select(x => x.Position).Should()
            .ContainInOrder(new CellPosition(1, 3), new CellPosition(9, 0));
    }

    [Test]
    public void Moved_Vertex_Keeps_Dependency_On_Adjusted_Reference()
    {
        Set(1, 2, "=1"); // C2
        Set(1, 0, "=C2"); // A2 - in the band but before the insert
        Set(1, 4, "=C2"); // E2 - in the band and moves too

        _dm.InsertCellsAt(new Region(1, 1, 1, 1), Axis.Col, "Sheet1");

        FormulaAt(1, 0).Should().Be("=D2");
        FormulaAt(1, 5).Should().Be("=D2");
        HasEdge(1, 3, 1, 0).Should().BeTrue();
        HasEdge(1, 3, 1, 5).Should().BeTrue();
    }

    [Test]
    public void Named_Formulas_Follow_The_Band_Rule()
    {
        _dm.SetFormula("x", GetFormula("=B2"));
        _dm.SetFormula("y", GetFormula("=sum(A1:C5)"));

        var restore = _dm.InsertCellsAt(new Region(1, 2, 1, 1), Axis.Col, "Sheet1");
        _dm.GetFormula("x")!.ToFormulaString().Should().Be("=C2");
        _dm.GetFormula("y")!.ToFormulaString().Should().Be("=sum(A1:C5)");

        _dm.Restore(restore);
        _dm.GetFormula("x")!.ToFormulaString().Should().Be("=B2");
    }

    [Test]
    public void Insert_And_Remove_Cells_Restore_Round_Trip()
    {
        Set(1, 1, "=1"); // B2
        Set(1, 2, "=B2"); // C2
        Set(2, 3, "=sum(B2:D3)"); // D3
        Set(9, 0, "=C2+sum(A1:C5)+D3"); // A10
        Set(0, 0, "=Sheet1!C2+Sheet1!B3", "Sheet2");

        var insert = _dm.InsertCellsAt(new Region(1, 2, 1, 1), Axis.Col, "Sheet1"); // B2:B3 shift right
        var remove = _dm.RemoveCellsAt(new Region(1, 1, 2, 2), Axis.Row, "Sheet1"); // C2 shift up

        _dm.Restore(remove);
        _dm.Restore(insert);

        FormulaAt(1, 1).Should().Be("=1");
        FormulaAt(1, 2).Should().Be("=B2");
        FormulaAt(2, 3).Should().Be("=sum(B2:D3)");
        FormulaAt(9, 0).Should().Be("=C2+sum(A1:C5)+D3");
        FormulaAt(0, 0, "Sheet2").Should().Be("=Sheet1!C2+Sheet1!B3");
        _dm.FormulaCount.Should().Be(5);

        HasEdge(1, 1, 1, 2).Should().BeTrue();
        HasEdge(1, 1, 2, 3).Should().BeTrue();
        HasEdge(1, 2, 2, 3).Should().BeTrue();
        HasEdge(1, 2, 9, 0).Should().BeTrue();
        HasEdge(2, 3, 9, 0).Should().BeTrue();

        _dm.GetDirectDependents(new Region(1, 2), "Sheet1").Distinct().Count().Should().Be(3);
        _dm.GetDirectDependents(new Region(2, 1), "Sheet1").Distinct().Count().Should().Be(3);
        _dm.HasDependents(new Region(1, 3), "Sheet1").Should().BeTrue(); // D2 - only by B2:D3
        _dm.GetDirectDependents(new Region(1, 4), "Sheet1").Should().BeEmpty();
        _dm.GetDirectDependents(new Region(1, 3), "Sheet1").Distinct().Count().Should().Be(1);
    }
}
