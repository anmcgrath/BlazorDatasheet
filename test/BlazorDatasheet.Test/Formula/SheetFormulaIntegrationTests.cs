using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Functions.Logical;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Internal.Commands;

namespace BlazorDatasheet.Test.Formula;

public class SheetFormulaIntegrationTests
{
    private Sheet _sheet;

    [SetUp]
    public void TestSetup()
    {
        _sheet = new Sheet(10, 10);
    }

    [Test]
    public void Accept_Edit_With_Formula_String_Sets_Formula()
    {
        _sheet.Cells.SetValue(0, 0, 5);
        _sheet.Editor.BeginEdit(1, 1);
        _sheet.Editor.EditValue = "=A1 + 10";
        _sheet.Editor.AcceptEdit();

        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        _sheet.Cells.SetValue(0, 0, 5);
        var formulaVal = _sheet.Cells.GetValue(1, 1);
        Assert.AreEqual(15, formulaVal);
    }

    [Test]
    public void Set_Formula_Then_Undo_Removes_Formula()
    {
        _sheet.Cells.SetFormula(0, 0, "=5");
        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(0, 0).Should().BeNull();
    }

    [Test]
    public void Formula_Calculation_Performs_When_Referenced_Cell_Value_Changes()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1 + 10");
        _sheet.Cells.SetValue(0, 0, 5);
        Assert.AreEqual(15, _sheet.Cells.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Formula_Is_Set()
    {
        _sheet.Cells.SetValue(0, 0, 5);
        _sheet.Cells.SetFormula(1, 1, "=A1 + 10");
        Assert.AreEqual(15, _sheet.Cells.GetValue(1, 1));
    }

    [Test]
    public void Formula_Calculation_Performs_When_Formula_Is_Set_Over_Formula()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.Cells.SetFormula(1, 0, "=A1");
        _sheet.Cells.SetFormula(2, 0, "=A2");
        _sheet.Cells.SetFormula(1, 0, "=A1");
        _sheet.Cells[2, 0].Value.Should().Be(2);
    }

    [Test]
    public void Setting_Cell_Value_Will_Clear_Formula()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1");
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));

        // Set sheet cell (1, 1) to any old value and the formula should be cleared.
        _sheet.Cells.SetValue(1, 1, "Blah");
        Assert.IsFalse(_sheet.Cells.HasFormula(1, 1));
        _sheet.FormulaEngine.IsCellReferenced(0, 0).Should().BeFalse();
    }

    [Test]
    public void Clear_Sheet_Cell_Will_Clear_Formula()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1");
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        _sheet.Cells.ClearCells(new Region(1, 1));
        Assert.IsFalse(_sheet.Cells.HasFormula(1, 1));
        _sheet.FormulaEngine.GetDependencyInfo().Count().Should().Be(0);
    }

    [Test]
    public void Setting_Invalid_Formula_Will_Not_Set_Formula()
    {
        var invalidFormulaString = "=.A1";
        _sheet.Editor.BeginEdit(0, 0);
        _sheet.Editor.EditValue = invalidFormulaString;
        _sheet.Editor.AcceptEdit();
        Assert.False(_sheet.Cells.HasFormula(0, 0));
    }

    [Test]
    public void Set_Cell_Value_Over_Formula_Using_Command_Restores_On_Undo()
    {
        _sheet.Cells.SetValue(1, 1, "Test");
        _sheet.Cells.SetFormula(1, 1, "=10");
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        Assert.AreEqual(10, _sheet.Cells.GetValue(1, 1));
        _sheet.Commands.ExecuteCommand(new SetCellValueCommand(1, 1, "TestChange"));
        _sheet.Commands.Undo();
        Assert.AreEqual(10, _sheet.Cells.GetValue(1, 1));
        Assert.IsTrue(_sheet.Cells.HasFormula(1, 1));
        Assert.AreEqual("=10", _sheet.Cells.GetFormulaString(1, 1));
    }

    [Test]
    public void Clear_Cell_Value_Using_Command_Restores_Formula_On_Undo()
    {
        _sheet.Cells.SetFormula(1, 1, "=10");
        _sheet.Commands.ExecuteCommand(new ClearCellsCommand(_sheet.Range(1, 1)));
        Assert.False(_sheet.Cells.HasFormula(1, 1));
        _sheet.Commands.Undo();
        Assert.True(_sheet.Cells.HasFormula(1, 1));
        Assert.AreEqual("=10", _sheet.Cells.GetFormulaString(1, 1));
    }

    [Test]
    public void Sum_On_Empty_Cell_Treats_Empty_Cell_As_Zero()
    {
        _sheet.Cells.SetFormula(1, 1, "=A1 + 5");
        var val = _sheet.Cells.GetValue(1, 1);
        val.Should().Be(5);
    }

    [Test]
    public void Set_Cell_Formula_Over_Value_Then_Undo_Restores_Value()
    {
        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.SetFormula(0, 0, "=5");
        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(0, 0).Should().Be(10);
    }


    [Test]
    public void FormulaEngine_Set_Variable_Calculates()
    {
        _sheet.FormulaEngine.SetVariable("x", 10);
        _sheet.Cells.SetFormula(1, 1, "=x");
        _sheet.Cells.GetValue(1, 1).Should().Be(10);
    }

    [Test]
    public void Formula_Referencing_Range_With_Formula_Recalcs_When_Formula_Recalcs()
    {
        // Cell A1 = 10
        // Cell A2 = 20
        // Cell A3 = Sum(A1:A2)
        // Cell A4 = Sum(A2:A3)
        // When we set A1 and A2, A3 should evaluate first and then A4 because the result of A4 depends on A3
        _sheet.Cells.SetFormula(2, 0, "=AVERAGE(A1:A2)"); // A3 = Sum(A1:A2)
        _sheet.Cells.SetFormula(3, 0, "=AVERAGE(A2:A3)"); //A4 = Sum(A2:A3)
        _sheet.Cells.SetValue(0, 0, 10); // A1 = 10
        _sheet.Cells.SetValue(1, 0, 20); // A2 = 20
        _sheet.Cells.GetValue(2, 0).Should().Be((10 + 20) / 2d); // A3 = Sum(A1:A2) = 15
        _sheet.Cells.GetValue(3, 0).Should().Be((20 + 15) / 2d); // A4 = Sum(A2:A3) = 17.5
    }

    [Test]
    public void Formula_Referencing_Deleted_Formula_Updates_When_Formula_Is_Cleared_Then_Value_Changes()
    {
        _sheet.Cells.SetFormula(0, 0, "=A2");
        _sheet.Cells.SetFormula(0, 1, "=A1");
        _sheet.Cells.ClearCells(new Region(0, 0));
        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.GetCellValue(0, 1).GetValue<int>().Should().Be(10);
    }

    [Test]
    public void Formula_Referencing_Deleted_Formula_Updates_When_Formula_Has_Value_Set_Over_It()
    {
        _sheet.Cells.SetFormula(0, 0, "=A2");
        _sheet.Cells.SetFormula(0, 1, "=A1");

        // now override the formula in A1, the formula in B1 (0,1) should update to the new value
        _sheet.Cells.SetValue(0, 0, 10);

        _sheet.Cells.GetCellValue(0, 1).GetValue<int>().Should().Be(10);
    }

    [Test]
    public void Sheet_Should_Not_Recalculate_If_Formula_Removed_From_Sheet()
    {
        _sheet.Cells[0, 0].Formula = "=B1"; // set A1 = B1
        _sheet.Cells[1, 0].Formula = "=A1"; // set A2 = A1
        // override formula at A1 - should remove links from A1 -> B1
        _sheet.Cells.SetValue(0, 0, string.Empty);
        // set value at b1 and ensure sheet doesn't calculate

        int changeCount = 0;

        _sheet.Cells.CellsChanged += (sender, args) => { changeCount++; };
        // change B1
        _sheet.Cells[0, 1].Value = 2; // Set b1 = 2 (shouldn't recalc!) 

        changeCount.Should().Be(1);
    }

    [Test]
    public void Insert_Row_Before_Formula_Shifts_Formula_And_Updates_Ref()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(2, 2, "=B2"); // set C3 to =B2

        sheet.Rows.InsertAt(1);
        sheet.Cells[2, 2].Formula.Should().BeNull();
        sheet.Cells[3, 2].Formula.Should().Be("=B3");

        sheet.FormulaEngine.IsCellReferenced(2, 1).Should().BeTrue(); // B3
        sheet.FormulaEngine.IsCellReferenced(1, 1).Should().BeFalse(); // B2

        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=B2");

        sheet.FormulaEngine.IsCellReferenced(1, 1).Should().BeTrue(); // B2
    }


    [Test]
    public void Insert_Rows_Shifts_References()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "=A5");
        sheet.Rows.InsertAt(2, 2);
        sheet.FormulaEngine.IsCellReferenced(4, 0).Should().BeFalse(); // a5
        sheet.FormulaEngine.IsCellReferenced(6, 0).Should().BeTrue();
        sheet.Cells.GetFormulaString(0, 0).Should().BeEquivalentTo("=A7");
    }

    [Test]
    public void Insert_Col_Before_Formula_Shifts_Formula_And_Updates_Ref()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(2, 2, "=B2");
        sheet.Columns.InsertAt(1);
        sheet.Cells[2, 2].Formula.Should().BeNull();
        sheet.Cells[2, 3].Formula.Should().Be("=C2");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=B2");
    }

    [Test]
    public void Remove_Row_Before_Formula_Shifts_Formula_And_Updates_Ref()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(2, 2, "=B2");
        sheet.Rows.RemoveAt(0);
        sheet.Cells[2, 2].Formula.Should().BeNull();
        sheet.Cells[1, 2].Formula.Should().Be("=B1");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=B2");
    }

    [Test]
    public void Remove_Col_Before_Formula_Shifts_Formula_And_Updates_Ref()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(2, 2, "=B2");
        sheet.Columns.RemoveAt(0);
        sheet.Cells[2, 2].Formula.Should().BeNull();
        sheet.Cells[2, 1].Formula.Should().Be("=A2");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=B2");
    }

    [Test]
    public void Insert_Row_Before_Formula_Reference_Shifts_Formula_Reference()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(2, 2, "=D5");
        sheet.Rows.InsertAt(3, 2);
        sheet.Cells[2, 2].Formula.Should().Be("=D7");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=D5");
    }

    [Test]
    public void Insert_Col_Before_Formula_Reference_Shifts_Formula_Reference()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(2, 2, "=D5");
        sheet.Columns.InsertAt(3, 2);
        sheet.Cells[2, 2].Formula.Should().Be("=F5");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=D5");
    }

    [Test]
    public void Insert_Row_Into_Referenced_Range_Expands_Formula_Reference()
    {
        var sheet = new Sheet(20, 20);
        sheet.Cells.SetFormula(2, 2, "=sum(D5:D10)");
        sheet.Rows.InsertAt(6, 2);
        sheet.Cells[2, 2].Formula.Should().Be("=sum(D5:D12)");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=sum(D5:D10)");
    }

    [Test]
    public void Insert_Row_Before_Formula_Allows_Correct_Calculation()
    {
        var sheet = new Sheet(20, 20);
        sheet.Cells.SetFormula(5, 0, "=A1");
        sheet.Rows.InsertAt(2, 1);
        sheet.Range("A1").Value = 2;
        sheet.Cells[6, 0].Value.Should().Be(2);
    }

    [Test]
    public void Insert_Row_Bug_Produces_Multiple_Formula()
    {
        var sheet = new Sheet(20, 20);
        sheet.Cells.SetFormula(5, 0, "=A1");
        sheet.Rows.InsertAt(2, 1);
        sheet.Editor.BeginEdit(6, 0);
        sheet.Editor.EditValue = "=A1";
        sheet.Editor.AcceptEdit();
        sheet.FormulaEngine.GetCalculationOrder().Count().Should().Be(1);
    }

    [Test]
    public void Remove_Row_Into_Referenced_Range_Contracts_Formula_Reference()
    {
        var sheet = new Sheet(20, 20);
        sheet.Cells.SetFormula(2, 2, "=sum(D5:D10)");
        sheet.Rows.RemoveAt(4, 2);
        sheet.Cells[2, 2].Formula.Should().Be("=sum(D5:D8)");
        sheet.Commands.Undo();
        sheet.Cells[2, 2].Formula.Should().Be("=sum(D5:D10)");
    }

    [Test]
    public void Remove_Formula_And_Undo_Restores_Dependencies()
    {
        var sheet = new Sheet(20, 20);
        sheet.Cells.SetFormula(1, 1, "=5");
        sheet.Cells.SetFormula(0, 0, "=B2");
        sheet.Cells.ClearCells(new Region(0, 0));
        sheet.Commands.Undo();
        sheet.Cells.GetCellValue(0, 0).GetValue<int>().Should().Be(5);
        sheet.FormulaEngine.IsCellReferenced(1, 1).Should().BeTrue();
    }

    [Test]
    public void Remove_Formula_In_Column_Removes_And_Restores_Correctly()
    {
        _sheet.Cells.SetFormula(0, 0, "=A2");
        _sheet.FormulaEngine.IsCellReferenced(1, 0).Should().BeTrue();
        _sheet.Columns.RemoveAt(0);
        _sheet.FormulaEngine.IsCellReferenced(1, 0).Should().BeFalse();
        _sheet.Commands.Undo();
        _sheet.Cells[0, 0].Formula.Should().Be("=A2");
        _sheet.FormulaEngine.IsCellReferenced(1, 0).Should().BeTrue();
    }

    [Test]
    public void Conditional_Circular_Reference_Calculates_Correctly()
    {
        // subset of https://github.com/anmcgrath/BlazorDatasheet/issues/126
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 1, "=if(A1<A3,A1, A1+B2)");
        sheet.Cells.SetFormula(1, 1, "=if(A1<A3,A2+B1,A2+B3)");
        sheet.Cells.SetFormula(2, 1, "=if(A1<A3,A3+B2,A3+B4)");
        sheet.Cells.SetValues(0, 0, [[1], [3], [5]]);
        sheet.Cells[0, 1].Value.Should().Be(1);
        sheet.Cells[1, 1].Value.Should().Be(4);
        sheet.Cells[2, 1].Value.Should().Be(9);
        sheet.BatchUpdates();
        sheet.SortRange(new ColumnRegion(0), [new(0, false)]);
        sheet.EndBatchUpdates();
        sheet.Cells[0, 1].Value.Should().Be(9);
        sheet.Cells[1, 1].Value.Should().Be(4);
        sheet.Cells[2, 1].Value.Should().Be(1);
    }

    [Test]
    public void Simple_Non_Circular_Strongly_Grouped_Reference_Calculates_Correctly()
    {
        var env = new TestEnvironment();
        env.RegisterFunction("if", new IfFunction());
        var eval = new Evaluator(env);
        var parser = new Parser();
        var fA1 = new CellFormula(parser.Parse("=B1"));
        var fB1 = new CellFormula(parser.Parse("=if(true,C1,A1)"));
        env.SetCellValue(0, 2, 3);
        env.SetCellFormula(0, 0, fA1);
        env.SetCellFormula(0, 1, fB1);
        var res = eval.Evaluate(fB1);
        res.Data.Should().Be(3);
    }

    [Test]
    public void Circular_Ref_Returns_Circ_Error()
    {
        var sheet = new Sheet(20, 20);
        sheet.Cells.SetFormula(0, 0, "=A2");
        sheet.Cells.SetFormula(1, 0, "=A1");
        sheet.Cells.GetCellValue(0, 0).IsError().Should().BeTrue();
        sheet.Cells.GetCellValue(1, 0).IsError().Should().BeTrue();
    }

    [Test]
    public void Range_Operator_On_Range_Will_Update_Correctly_When_Sheet_Edited()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(5, 5, "=sum(a1:a2:b3)");
        sheet.Cells.SetValues(0, 0, [[1, 2], [3, 4]]);
        sheet.Cells.GetValue(5, 5).Should().Be(1 + 2 + 3 + 4);
        sheet.Range("b1")!.Value = 3;

        // update B2
        sheet.Cells.GetValue(5, 5).Should().Be(1 + 3 + 3 + 4);
    }

    [Test]
    [TestCase("=1%")]
    [TestCase("=+1")]
    public void Formula_String_Is_Correct_After_Parsing(string formula)
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, formula);
        sheet.Cells.GetFormulaString(0, 0).Should().Be(formula);
    }

    [Test]
    public void Set_Variable_With_Formula_Will_Update_When_Cell_Reference_Changes()
    {
        var sheet = new Sheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", "=A1");
        sheet.Range("A1")!.Value = "Test";
        sheet.FormulaEngine.GetVariable("x").Data.Should().Be("Test");
    }

    [Test]
    public void Set_Variable_With_Formula_Will_Calculate_Correctly_When_Referenced()
    {
        var sheet = new Sheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", "=A1");
        sheet.Cells.SetFormula(1, 0, "=x+1");
        sheet.Range("A1")!.Value = 2;
        sheet.Cells[1, 0].Value.Should().Be(3);
    }

    [Test]
    public void Set_Variable_Raises_args()
    {
        var sheet = new Sheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", "Test");
        using var monitor = sheet.FormulaEngine.Monitor();
        sheet.FormulaEngine.SetVariable("x", "Test2");

        monitor.Should()
            .Raise(nameof(sheet.FormulaEngine.VariableChanged))
            .WithArgs<VariableChangedEventArgs>(args =>
                (args.NewValue!.Data == "Test2" && args.PreviousValue!.Data == "Test" && args.VariableName == "x"));

        monitor.Clear();
    }

    [Test]
    public void Set_Formula_Then_Undo_Has_Correct_References()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetFormula(0, 0, "=C3");
        sheet.Cells.SetFormula(1, 2, "=A1");
        sheet.FormulaEngine.GetDependencyInfo().Count().Should().Be(3);
        sheet.Commands.Undo();
        var di = sheet.FormulaEngine.GetDependencyInfo();
        sheet.FormulaEngine.GetDependencyInfo().Count().Should().Be(1);
        sheet.Commands.Undo();
        var refs = sheet.FormulaEngine.GetDependencyInfo();
        sheet.FormulaEngine.GetDependencyInfo().Count().Should().Be(0);
    }

    [Test]
    public void Override_Formula_With_Formula_Then_Undo_Will_Set_Original_References()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetFormula(0, 0, "=C3");
        sheet.Cells.SetFormula(0, 0, "=A5");
        sheet.Commands.Undo();
        sheet.FormulaEngine.GetDependencyInfo().Count().Should().Be(1);
    }

    [Test]
    public void Insert_Rows_Shifts_Formula_With_Correct_Calculation_Order()
    {
        _sheet.Cells.SetFormula(1, 0, "=A5");
        _sheet.Cells.SetFormula(4, 0, "=1");
        _sheet.Rows.InsertAt(0, 2);
        var di = _sheet.FormulaEngine.GetCalculationOrder();

        di
            .SelectMany(x => x)
            .Select(x => x.Key)
            .Should()
            .BeEquivalentTo(["A7", "A4"]);
    }
}