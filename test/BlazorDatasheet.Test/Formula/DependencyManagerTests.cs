using System;
using System.Linq;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class DependencyManagerTests
{
    private DependencyManager _dm;

    [SetUp]
    public void Setup_Tests()
    {
        _dm = new DependencyManager();
        _dm.AddSheet("Sheet1");
        _dm.AddSheet("Sheet2");
    }

    private CellFormula GetFormula(string formulaStr)
    {
        var parser = new Parser(new TestEnvironment());
        return parser.FromString(formulaStr);
    }

    [Test]
    public void SetFormula()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2"));
        _dm.HasDependents(1, 0, "Sheet1").Should().BeTrue();
        _dm.ClearFormula(0, 0, "Sheet1");
        _dm.HasDependents(1, 0, "Sheet1").Should().BeFalse();
    }

    [Test]
    public void Calculation_Order_Correct()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2")); // A1
        _dm.SetFormula(1, 0, "Sheet1", GetFormula("=A3")); // A2
        _dm.GetCalculationOrder()
            .SelectMany(x => x)
            .Select(x => new CellPosition(x.Row, x.Col))
            .Should()
            .BeEquivalentTo([
                new CellPosition(1, 0), // a2
                new CellPosition(0, 0), // a1
            ]);
    }

    [Test]
    public void Calculation_Order_Correct_With_Region()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=B1 + sum(C1:D3)")); // A1
        _dm.SetFormula(0, 1, "Sheet1", GetFormula("=A2")); // B1
        _dm.SetFormula(0, 2, "Sheet1", GetFormula("=A3")); // C1

        var sorted = _dm.GetCalculationOrder().SelectMany(x => x)
            .Where(x => x.Formula != null)
            .Select(x => x.Col)
            .ToList();

        sorted.Should().BeEquivalentTo([2, 1, 0]);
    }

    [Test]
    public void Insert_Rows_Shifts_References()
    {
        var f = GetFormula("=A5");
        _dm.SetFormula(0, 0, "Sheet1", f);
        _dm.InsertRowColAt(2, 2, Axis.Row, "Sheet1");
        _dm.HasDependents(4, 0, "Sheet1").Should().BeFalse(); // A5
        _dm.HasDependents(6, 0, "Sheet1").Should().BeTrue(); // A7
        f.ToFormulaString().Should().BeEquivalentTo("=A7");
    }

    [Test]
    public void Insert_Rows_Shifts_Formula()
    {
        var f = GetFormula("=A5");
        _dm.SetFormula(1, 0, "Sheet1", f);
        _dm.InsertRowColAt(0, 2, Axis.Row, "Sheet1");
        _dm.GetCalculationOrder()
            .SelectMany(x => x)
            .Should().BeEquivalentTo([new FormulaVertex(3, 0, "Sheet1", GetFormula("=A7"))]);
    }

    [Test]
    public void Set_Formula_And_Restore_Restores_Correctly()
    {
        var rest1 = _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2+sum(a10:A12)"));
        _dm.HasDependents(10, 0, "Sheet1").Should().BeTrue();
        var rest2 = _dm.SetFormula(1, 0, "Sheet1", GetFormula("=B10"));
        _dm.HasDependents(9, 1, "Sheet1").Should().BeTrue();
        _dm.Restore(rest2);
        _dm.HasDependents(9, 1, "Sheet1").Should().BeFalse();
        _dm.Restore(rest1);
        _dm.HasDependents(10, 0, "Sheet1").Should().BeFalse();
        _dm.GetCalculationOrder().Should().BeEmpty();
    }

    [Test]
    public void Clear_Formula_And_Restore_Restores_Correctly()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2"));
        var rest = _dm.ClearFormula(0, 0, "Sheet1");
        _dm.HasDependents(1, 0, "Sheet1").Should().BeFalse();
        _dm.Restore(rest);
        _dm.HasDependents(1, 0, "Sheet1").Should().BeTrue();
        _dm.GetCalculationOrder().SelectMany(x => x).Select(x => x.Position)
            .Should()
            .BeEquivalentTo([new CellPosition(0, 0)]);
    }

    [Test]
    public void Rename_Sheet_With_Existing_Formulas_On_Sheet_Does_Not_Throw_And_Keeps_Dependencies()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2"));
        _dm.SetFormula(1, 0, "Sheet1", GetFormula("=Sheet2!A1"));

        Action action = () => _dm.RenameSheet("Sheet1", "Renamed");

        action.Should().NotThrow();
        _dm.GetVertex(0, 0, "Renamed").Should().NotBeNull();
        _dm.GetVertex(1, 0, "Renamed").Should().NotBeNull();
        _dm.GetVertex(1, 0, "Renamed")!.Formula!.ToFormulaString().Should().Be("=Sheet2!A1");
        _dm.HasDependents(1, 0, "Renamed").Should().BeTrue();
    }

    [Test]
    public void Rename_Sheet_Then_Insert_Shifts_Formula_Vertex_Position()
    {
        _dm.SetFormula(1, 0, "Sheet1", GetFormula("=A1"));
        _dm.RenameSheet("Sheet1", "Renamed");

        _dm.InsertRowColAt(1, 2, Axis.Row, "Renamed");

        _dm.GetVertex(3, 0, "Renamed").Should().NotBeNull();
        _dm.GetVertex(1, 0, "Renamed").Should().BeNull();
    }

    [Test]
    public void Clear_Formula_With_Multi_Sheet_References_And_Restore_Restores_All_Referenced_Stores()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2+Sheet2!A2"));
        _dm.HasDependents(1, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeTrue();

        var restoreData = _dm.ClearFormula(0, 0, "Sheet1");
        _dm.HasDependents(1, 0, "Sheet1").Should().BeFalse();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeFalse();

        _dm.Restore(restoreData);
        _dm.HasDependents(1, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeTrue();
    }

    [Test]
    public void Clear_Formula_With_Multiple_References_In_Same_Sheet_Restore_Restores_All_References()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A2+A3"));
        _dm.HasDependents(1, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(2, 0, "Sheet1").Should().BeTrue();

        var restoreData = _dm.ClearFormula(0, 0, "Sheet1");
        _dm.HasDependents(1, 0, "Sheet1").Should().BeFalse();
        _dm.HasDependents(2, 0, "Sheet1").Should().BeFalse();

        _dm.Restore(restoreData);
        _dm.HasDependents(1, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(2, 0, "Sheet1").Should().BeTrue();
    }

    [Test]
    public void Insert_Shift_And_Restore_Roundtrip_Restores_References_And_Dependencies()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A3+Sheet2!A2"));
        _dm.HasDependents(2, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeTrue();

        var shiftRestore = _dm.InsertRowColAt(1, 2, Axis.Row, "Sheet1");
        _dm.GetVertex(0, 0, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=A5+Sheet2!A2");

        _dm.Restore(shiftRestore);
        _dm.GetVertex(0, 0, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=A3+Sheet2!A2");
        _dm.HasDependents(2, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeTrue();
    }

    [Test]
    public void Remove_Shift_And_Restore_Roundtrip_Restores_References_And_Dependencies()
    {
        _dm.SetFormula(0, 0, "Sheet1", GetFormula("=A5+Sheet2!A2"));
        _dm.HasDependents(4, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeTrue();

        var shiftRestore = _dm.RemoveRowColAt(1, 2, Axis.Row, "Sheet1");
        _dm.GetVertex(0, 0, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=A3+Sheet2!A2");

        _dm.Restore(shiftRestore);
        _dm.GetVertex(0, 0, "Sheet1")!.Formula!.ToFormulaString().Should().Be("=A5+Sheet2!A2");
        _dm.HasDependents(4, 0, "Sheet1").Should().BeTrue();
        _dm.HasDependents(1, 0, "Sheet2").Should().BeTrue();
    }
}
