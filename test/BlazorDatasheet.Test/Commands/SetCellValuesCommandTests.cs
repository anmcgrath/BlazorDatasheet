using System;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetCellValuesCommandTests
{
    [Test]
    public void Set_Cell_Values_Respects_Cell_Type()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValues(0, 0, [["2020-09-09"]]);
        // ensure the conversion happens without setting type first
        sheet.Cells.GetValue(0, 0).Should().BeOfType<DateTime>();
        sheet.Cells.SetType(0, 0, "text");
        // now since the type is "text" the conversion should not happen
        sheet.Cells.SetValues(0, 0, [["2020-09-09"]]);
        sheet.Cells.GetValue(0, 0).Should().BeOfType<string>();
    }

    [Test]
    public void Set_Cell_Value_Respects_Cell_Type()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 0, "2020-09-09");
        // ensure the conversion happens without setting type first
        sheet.Cells.GetValue(0, 0).Should().BeOfType<DateTime>();
        sheet.Cells.SetType(0, 0, "text");
        // now since the type is "text" the conversion should not happen
        sheet.Cells.SetValue(0, 0, "2020-09-09");
        sheet.Cells.GetValue(0, 0).Should().BeOfType<string>();
    }

    [Test]
    public void Set_Rectangular_Cell_Values_Clears_Formulas_And_Restores_On_Undo()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetFormula(0, 0, "=1+1");
        sheet.Cells.SetFormula(0, 1, "=2+2");

        sheet.Cells.SetValues(0, 0, [[CellValue.Number(10), CellValue.Number(20)]]);

        sheet.Cells.GetFormula(0, 0).Should().BeNull();
        sheet.Cells.GetFormula(0, 1).Should().BeNull();
        sheet.Cells.GetValue(0, 0).Should().Be(10d);
        sheet.Cells.GetValue(0, 1).Should().Be(20d);

        sheet.Commands.Undo();

        sheet.Cells.GetFormulaString(0, 0).Should().Be("=1+1");
        sheet.Cells.GetFormulaString(0, 1).Should().Be("=2+2");
    }

    [Test]
    public void Set_Rectangular_Cell_Values_Restores_Previous_Values_On_Undo()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValues(0, 0, [[1, 2], [3, 4]]);
        sheet.Cells.SetValues(0, 0, [[5, 6], [7, 8]]);

        sheet.Cells.GetValue(1, 1).Should().Be(8d);

        sheet.Commands.Undo();

        sheet.Cells.GetValue(0, 0).Should().Be(1d);
        sheet.Cells.GetValue(0, 1).Should().Be(2d);
        sheet.Cells.GetValue(1, 0).Should().Be(3d);
        sheet.Cells.GetValue(1, 1).Should().Be(4d);
    }
}
