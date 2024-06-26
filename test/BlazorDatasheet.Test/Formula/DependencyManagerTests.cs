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
    private CellFormula GetFormula(string formulaStr)
    {
        var parser = new Parser();
        return parser.FromString(formulaStr);
    }

    [Test]
    public void SetFormula()
    {
        var dm = new DependencyManager();
        dm.SetFormula(0, 0, GetFormula("=A2"));
        dm.HasDependents(1, 0).Should().BeTrue();
        dm.ClearFormula(0, 0);
        dm.HasDependents(1, 0).Should().BeFalse();
    }

    [Test]
    public void Calculation_Order_Correct()
    {
        var dm = new DependencyManager();
        dm.SetFormula(0, 0, GetFormula("=A2")); // A1
        dm.SetFormula(1, 0, GetFormula("=A3")); // A2
        dm.GetCalculationOrder()
            .Select(x => new CellPosition(x.Region!.Top, x.Region!.Left))
            .Should()
            .BeEquivalentTo([
                new CellPosition(1, 0), // a2
                new CellPosition(0, 0), // a1
            ]);
    }

    [Test]
    public void Calculation_Order_Correct_With_Region()
    {
        var dm = new DependencyManager();
        dm.SetFormula(0, 0, GetFormula("=B1 + sum(C1:D3)")); // A1
        dm.SetFormula(0, 1, GetFormula("=A2")); // B1
        dm.SetFormula(0, 2, GetFormula("=A3")); // C1

        var sorted = dm.GetCalculationOrder()
            .Where(x => x.Formula != null)
            .Select(x => x.Region!.Left)
            .ToList();

        sorted.Should().BeEquivalentTo([2, 1, 0]);
    }

    [Test]
    public void Insert_Rows_Shifts_References()
    {
        var dm = new DependencyManager();
        var f = GetFormula("=A5");
        dm.SetFormula(0, 0, f);
        dm.InsertRowColAt(2, 2, Axis.Row);
        dm.HasDependents(4, 0).Should().BeFalse(); // A5
        dm.HasDependents(6, 0).Should().BeTrue(); // A7
        f.ToFormulaString().Should().BeEquivalentTo("=A7");
    }

    [Test]
    public void Insert_Rows_Shifts_Formula()
    {
        var dm = new DependencyManager();
        var f = GetFormula("=A5");
        dm.SetFormula(1, 0, f);
        dm.InsertRowColAt(0, 2, Axis.Row);
        dm.GetCalculationOrder()
            .Should().BeEquivalentTo([new FormulaVertex(3, 0, GetFormula("=A7"))]);
    }

    [Test]
    public void Set_Formula_And_Restore_Restores_Correctly()
    {
        var dm = new DependencyManager();
        var rest1 = dm.SetFormula(0, 0, GetFormula("=A2+sum(a10:A12)"));
        dm.HasDependents(10, 0).Should().BeTrue();
        var rest2 = dm.SetFormula(1, 0, GetFormula("=B10"));
        dm.HasDependents(9, 1).Should().BeTrue();
        dm.Restore(rest2);
        dm.HasDependents(9, 1).Should().BeFalse();
        dm.Restore(rest1);
        dm.HasDependents(10, 0).Should().BeFalse();
        dm.GetCalculationOrder().Should().BeEmpty();
    }

    [Test]
    public void Clear_Formula_And_Restore_Restores_Correctly()
    {
        var dm = new DependencyManager();
        dm.SetFormula(0, 0, GetFormula("=A2"));
        var rest = dm.ClearFormula(0, 0);
        dm.HasDependents(1, 0).Should().BeFalse();
        dm.Restore(rest);
        dm.HasDependents(1, 0).Should().BeTrue();
        dm.GetCalculationOrder().Select(x => x.Region)
            .Should()
            .BeEquivalentTo([new Region(0, 0)]);
    }
}