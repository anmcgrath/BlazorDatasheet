using System.Linq;
using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class NonEmptyEnumerationTests
{
    [Test]
    public void Non_Empty_Cells_Includes_A_Formula_Before_A_Cell_Holding_Only_MetaData()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetValue(1, 0, "label");
        sheet.Cells.SetFormula(1, 2, "=1");
        sheet.Cells.SetCellMetaData(1, 3, "marker", "observation");

        var row = sheet.Rows.NonEmpty.Single(r => r.RowIndex == 1);

        row.NonEmptyCells.Select(cell => cell.Col).Should().Equal(0, 2, 3);
    }

    [Test]
    public void Non_Empty_Rows_Includes_A_Row_Of_Cells_Before_A_Row_Holding_Only_A_Heading()
    {
        var sheet = new Sheet(5, 5);
        sheet.Cells.SetValue(0, 0, "label");
        sheet.Cells.SetFormula(2, 0, "=1");
        sheet.Rows.SetHeadings(4, 4, "totals");

        sheet.Rows.NonEmpty.Select(row => row.RowIndex).Should().Equal(0, 2, 4);
    }
}
