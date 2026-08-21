using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class DeepDependencyChainTests
{
    [Test]
    public void Long_Dependency_Chain_Recalculates_Without_Exhausting_The_Stack()
    {
        const int chainLength = 50_000;

        var sheet = new Sheet(chainLength + 1, 2);
        sheet.BatchUpdates();
        sheet.Cells.SetValue(0, 0, 1);
        for (var row = 1; row <= chainLength; row++)
            sheet.Cells.SetFormula(row, 0, $"=A{row}+1");
        sheet.EndBatchUpdates();

        sheet.Cells.GetCellValue(chainLength, 0).GetValue<double>().Should().Be(chainLength + 1);

        // dirty the root, forcing the whole chain to recalculate in dependency order
        sheet.Cells.SetValue(0, 0, 10);

        sheet.Cells.GetCellValue(chainLength, 0).GetValue<double>().Should().Be(chainLength + 10);
    }

    /// <summary>
    /// The recalculation order still has to be correct for a deep chain, not merely terminate.
    /// </summary>
    [Test]
    public void Long_Dependency_Chain_Calculates_In_Dependency_Order()
    {
        const int chainLength = 5_000;

        var sheet = new Sheet(chainLength + 1, 2);
        sheet.BatchUpdates();
        sheet.Cells.SetValue(0, 0, 0);
        for (var row = 1; row <= chainLength; row++)
            sheet.Cells.SetFormula(row, 0, $"=A{row}+1");
        sheet.EndBatchUpdates();

        for (var row = 0; row <= chainLength; row++)
            sheet.Cells.GetCellValue(row, 0).GetValue<double>().Should().Be(row);
    }

    /// <summary>
    /// A cycle in a chain still has to be reported as circular rather than looping forever.
    /// </summary>
    /// <remarks>
    /// The chain is kept short here on purpose. Closing the loop puts every cell into one strongly
    /// connected component, and Evaluator.EvaluateCellReference resolves references inside a group
    /// by recursing, so a long cycle exhausts the stack in the evaluator. That is a separate limit
    /// from the traversal in SccSort, which the tests above cover.
    /// </remarks>
    [Test]
    public void Cycle_In_A_Chain_Is_Detected()
    {
        const int chainLength = 50;

        var sheet = new Sheet(chainLength + 2, 2);
        sheet.BatchUpdates();
        for (var row = 1; row <= chainLength; row++)
            sheet.Cells.SetFormula(row, 0, $"=A{row}+1");

        // close the loop: A1 depends on the last cell of the chain
        sheet.Cells.SetFormula(0, 0, $"=A{chainLength + 1}");
        sheet.EndBatchUpdates();

        sheet.Cells.GetCellValue(0, 0).IsError().Should().BeTrue();
    }
}
