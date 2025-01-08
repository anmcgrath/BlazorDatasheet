using System;
using System.Linq;
using BlazorDatasheet.Core.Data;
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
    private Sheet _sheet = new(100, 100);
    private FormulaEngine formulaEngine;

    [SetUp]
    public void Setup()
    {
        formulaEngine = new FormulaEngine(_sheet);
    }

    private CellFormula GetFormula(string formulaStr)
    {
        var parser = new Parser();
        return parser.FromString(formulaStr);
    }

    [Test]
    public void SetFormula()
    {
        throw new NotImplementedException();
        formulaEngine.SetFormula(0, 0, GetFormula("=A2"));
        /*formulaEngine.IsReferenced(1, 0).Should().BeTrue();
        formulaEngine.RemoveFormula(0, 0);
        formulaEngine.IsReferenced(1, 0).Should().BeFalse();*/
    }

    [Test]
    public void Calculation_Order_Correct()
    {
        formulaEngine.SetFormula(0, 0, GetFormula("=A2")); // A1
        formulaEngine.SetFormula(1, 0, GetFormula("=A3")); // A2
        formulaEngine.GetCalculationOrder()
            .SelectMany(x => x)
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
        formulaEngine.SetFormula(0, 0, GetFormula("=B1 + sum(C1:D3)")); // A1
        formulaEngine.SetFormula(0, 1, GetFormula("=A2")); // B1
        formulaEngine.SetFormula(0, 2, GetFormula("=A3")); // C1

        var sorted = formulaEngine.GetCalculationOrder().SelectMany(x => x)
            .Where(x => x.Formula != null)
            .Select(x => x.Region!.Left)
            .ToList();

        sorted.Should().BeEquivalentTo([2, 1, 0]);
    }

    [Test]
    public void Insert_Rows_Shifts_References()
    {
        throw new NotImplementedException();
        var f = GetFormula("=A5");
        formulaEngine.SetFormula(0, 0, f);
        //formulaEngine.InsertRowColAt(2, 2, Axis.Row);
        /*formulaEngine.DependencyManager.IsReferenced(4, 0).Should().BeFalse(); // A5
        formulaEngine.DependencyManager.IsReferenced(6, 0).Should().BeTrue(); // A7*/
        f.ToFormulaString().Should().BeEquivalentTo("=A7");
    }

    [Test]
    public void Insert_Rows_Shifts_Formula()
    {
        throw new NotImplementedException();
        var f = GetFormula("=A5");
        formulaEngine.SetFormula(1, 0, f);
        /*formulaEngine.DependencyManager.InsertRowColAt(0, 2, Axis.Row);
        formulaEngine.DependencyManager.GetCalculationOrder()
            .SelectMany(x => x)
            .Should().BeEquivalentTo([new FormulaVertex(3, 0, GetFormula("=A7"))]);*/
    }
    
    [Test]
    public void Clear_Named_Reference_Removes_References_To_It()
    {
        formulaEngine.SetVariable("x", "=A2");
        formulaEngine.SetFormula(1, 0, GetFormula("=A1")); // x = A2 = A1
        // x is dependent on both the region A2 and the formula at A2 ("=A1")
        formulaEngine.GetDependencyInfo().Count().Should().Be(3);
        formulaEngine.ClearVariable("x");
        var dependencies = formulaEngine.GetDependencyInfo();
        dependencies.Count().Should().Be(1); // still have A2 = A1
    }
}