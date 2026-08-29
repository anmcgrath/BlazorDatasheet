using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class FormulaEngineTests
{
    [Test]
    public void GetReferences_ReturnsDetachedReferences()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells.SetValue(0, 1, 1);
        sheet.Cells.SetValue(0, 2, 2);
        sheet.Cells.SetFormula(0, 0, "=B1");

        var reference = sheet.FormulaEngine.GetReferences(0, 0, sheet.Name).Single();
        reference.Shift(0, 1);

        sheet.Cells.GetFormulaString(0, 0).Should().Be("=B1");
        sheet.Cells.SetValue(0, 2, 3);
        sheet.Cells.GetValue(0, 0).Should().Be(1);
        sheet.Cells.SetValue(0, 1, 4);
        sheet.Cells.GetValue(0, 0).Should().Be(4);
    }

    [Test]
    public void GetVariableReferences_ReturnsDetachedReferences()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        var formulaEngine = workbook.GetFormulaEngine();
        formulaEngine.SetVariable("x", "=Sheet1!A1");

        var reference = formulaEngine.GetVariableReferences("x").Single();
        reference.Shift(0, 1);

        formulaEngine.TryGetVariableFormula("x", out var formula).Should().BeTrue();
        formula.Should().Be("=Sheet1!A1");
    }

    [Test]
    public void CalculateSheet_WhenEvaluationThrows_ResetsIsCalculating()
    {
        var environment = new TestEnvironment();
        environment.RegisterFunction(ThrowingFunction.Descriptor);
        var formulaEngine = new BlazorDatasheet.Core.FormulaEngine.FormulaEngine(environment);
        CalculationCompletedEventArgs? completedArgs = null;
        formulaEngine.CalculationCompleted += (_, args) => completedArgs = args;

        Action action = () => formulaEngine.SetVariable("x", "=THROWFN()");

        action.Should().Throw<InvalidOperationException>();
        formulaEngine.IsCalculating.Should().BeFalse();
        completedArgs.Should().NotBeNull();
        completedArgs!.Succeeded.Should().BeFalse();
        completedArgs.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public void CalculateSheet_EmitsLifecycleEventsForNonEmptyPass()
    {
        var formulaEngine = new BlazorDatasheet.Core.FormulaEngine.FormulaEngine(new TestEnvironment());
        CalculationStartedEventArgs? startedArgs = null;
        CalculationCompletedEventArgs? completedArgs = null;
        var calculatingWhenStarted = false;
        var calculatingWhenCompleted = true;

        formulaEngine.CalculationStarted += (_, args) =>
        {
            startedArgs = args;
            calculatingWhenStarted = formulaEngine.IsCalculating;
        };
        formulaEngine.CalculationCompleted += (_, args) =>
        {
            completedArgs = args;
            calculatingWhenCompleted = formulaEngine.IsCalculating;
        };

        formulaEngine.SetVariable("x", "=10");

        startedArgs.Should().NotBeNull();
        startedArgs!.CalculateAll.Should().BeFalse();
        startedArgs.FormulaCount.Should().Be(1);
        calculatingWhenStarted.Should().BeTrue();
        completedArgs.Should().NotBeNull();
        completedArgs!.CalculateAll.Should().BeFalse();
        completedArgs.FormulaCount.Should().Be(1);
        completedArgs.Succeeded.Should().BeTrue();
        completedArgs.Exception.Should().BeNull();
        calculatingWhenCompleted.Should().BeFalse();
    }

    [Test]
    public void CalculateSheet_DoesNotEmitLifecycleEventsWhenThereIsNoWork()
    {
        var formulaEngine = new BlazorDatasheet.Core.FormulaEngine.FormulaEngine(new TestEnvironment());
        var startedCount = 0;
        var completedCount = 0;
        formulaEngine.CalculationStarted += (_, _) => startedCount++;
        formulaEngine.CalculationCompleted += (_, _) => completedCount++;

        formulaEngine.CalculateSheet(false);

        startedCount.Should().Be(0);
        completedCount.Should().Be(0);
    }

    [Test]
    public void SetAndClearVariable_EmitVariableChangedWithOldAndNewState()
    {
        var formulaEngine = new BlazorDatasheet.Core.FormulaEngine.FormulaEngine(new TestEnvironment());
        var changes = new List<VariableChangedEventArgs>();
        formulaEngine.VariableChanged += (_, args) => changes.Add(args);

        formulaEngine.SetVariable("x", new CellValue(10));
        formulaEngine.SetVariable("x", new CellValue(20));
        formulaEngine.ClearVariable("x");

        changes.Should().HaveCount(3);
        changes[0].ChangeKind.Should().Be(VariableChangeKind.Added);
        changes[0].OldValue.Should().BeNull();
        changes[0].NewValue.Should().Be(new CellValue(10));
        changes[1].ChangeKind.Should().Be(VariableChangeKind.Updated);
        changes[1].OldValue.Should().Be(new CellValue(10));
        changes[1].NewValue.Should().Be(new CellValue(20));
        changes[2].ChangeKind.Should().Be(VariableChangeKind.Removed);
        changes[2].OldValue.Should().Be(new CellValue(20));
        changes[2].NewValue.Should().BeNull();
    }

    [Test]
    public void FormulaVariableRecalculation_EmitsAfterDependentCellsAreUpdated()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        var formulaEngine = workbook.GetFormulaEngine();
        sheet.Cells.SetValue(0, 0, 1);
        formulaEngine.SetVariable("x", "=Sheet1!A1");
        sheet.Cells.SetFormula(0, 1, "=x");

        VariableChangedEventArgs? changedArgs = null;
        CellValue dependentValueWhenEmitted = default;
        var calculatingWhenEmitted = true;
        formulaEngine.VariableChanged += (_, args) =>
        {
            if (args.Name != "x")
                return;

            changedArgs = args;
            dependentValueWhenEmitted = sheet.Cells.GetCellValue(0, 1);
            calculatingWhenEmitted = formulaEngine.IsCalculating;
        };

        sheet.Cells.SetValue(0, 0, 2);

        changedArgs.Should().NotBeNull();
        changedArgs!.ChangeKind.Should().Be(VariableChangeKind.Recalculated);
        changedArgs.OldValue.Should().Be(new CellValue(1));
        changedArgs.NewValue.Should().Be(new CellValue(2));
        dependentValueWhenEmitted.Should().Be(new CellValue(2));
        calculatingWhenEmitted.Should().BeFalse();
    }

    [Test]
    public void Formula_Variable_Can_Reference_Other_Formula_Variables()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", CellValue.Number(10));
        sheet.FormulaEngine.SetVariable("y", "=x");
        var hasValue = sheet.FormulaEngine.TryGetVariable("y", out var value);
        hasValue.Should().BeTrue();
        value.Should().Be(CellValue.Number(10));
    }

    [Test]
    public void Formula_Variable_Can_Reference_Other_Formula_Variables_Does_Not_Throw_With_MixedCell_Ref()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", CellValue.Number(10));
        Assert.DoesNotThrow(() =>
        {
            sheet.FormulaEngine.SetVariable("y", "=x+Sheet1!A1");
            var hasValue = sheet.FormulaEngine.TryGetVariable("y", out var value);
        });
    }

    [Test]
    public void Self_Referencing_Formula_Variable_Returns_Circular_Error()
    {
        var workbook = new Workbook();
        var formulaEngine = workbook.GetFormulaEngine();

        formulaEngine.SetVariable("x", "=x");

        formulaEngine.TryGetVariable("x", out var value).Should().BeTrue();
        value.GetValue<FormulaError>().ErrorType.Should().Be(ErrorType.Circular);
    }

    [Test]
    public void Mutually_Referencing_Formula_Variables_Return_Circular_Error()
    {
        var workbook = new Workbook();
        var formulaEngine = workbook.GetFormulaEngine();
        formulaEngine.SetVariable("x", "=y");

        formulaEngine.SetVariable("y", "=x");

        formulaEngine.TryGetVariable("x", out var x).Should().BeTrue();
        formulaEngine.TryGetVariable("y", out var y).Should().BeTrue();
        x.GetValue<FormulaError>().ErrorType.Should().Be(ErrorType.Circular);
        y.GetValue<FormulaError>().ErrorType.Should().Be(ErrorType.Circular);
    }

    [Test]
    public void Mixed_Cell_And_Formula_Variable_Cycle_Returns_Circular_Error()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        var formulaEngine = workbook.GetFormulaEngine();
        sheet.Cells.SetFormula(0, 0, "=x");

        formulaEngine.SetVariable("x", "=Sheet1!A1");

        formulaEngine.TryGetVariable("x", out var variable).Should().BeTrue();
        variable.GetValue<FormulaError>().ErrorType.Should().Be(ErrorType.Circular);
        sheet.Cells.GetCellValue(0, 0).GetValue<FormulaError>().ErrorType.Should().Be(ErrorType.Circular);
    }

    [Test]
    public void Non_Circular_Formula_Variable_Chain_Calculates_Correctly()
    {
        var workbook = new Workbook();
        var formulaEngine = workbook.GetFormulaEngine();
        formulaEngine.SetVariable("x", "=10");
        formulaEngine.SetVariable("y", "=x");

        formulaEngine.SetVariable("z", "=y+1");

        formulaEngine.TryGetVariable("z", out var value).Should().BeTrue();
        value.Should().Be(CellValue.Number(11));
    }

    [Test]
    public void Formula_Variable_Throws_When_Referencing_Cell_Without_Sheet_Specification()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", CellValue.Number(10));
        Assert.Throws<Exception>(() =>
        {
            sheet.FormulaEngine.SetVariable("y", "=x+A1");
            var hasValue = sheet.FormulaEngine.TryGetVariable("y", out var value);
        });
    }

    [Test]
    public void Try_Get_Formula_Variable_Returns_Formula()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", "=4*4");
        var hasFormula = sheet.FormulaEngine.TryGetVariableFormula("x", out var formula);
        hasFormula.Should().BeTrue();
        formula.Should().BeEquivalentTo("=4*4");
    }

    [Test]
    public void Try_Get_Formula_Variable_With_No_Formula_Returns_False()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", "2");
        var hasFormula = sheet.FormulaEngine.TryGetVariableFormula("x", out var formula);
        hasFormula.Should().BeFalse();
    }
}

internal static class ThrowingFunction
{
    public static FunctionDescriptor Descriptor { get; } = new(
        "THROWFN",
        [],
        (_, _) => throw new InvalidOperationException("Boom"),
        acceptsErrors: false,
        isVolatile: false);
}
