using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.DataStructures.Geometry;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class CellShiftReferenceRegressionTests
{
    [TestCase(Axis.Row)]
    [TestCase(Axis.Col)]
    public void Moving_Whole_Axes_Preserves_References_Inside_Moved_Formulas(Axis axis)
    {
        var sheet = new Sheet(10, 10);
        var rows = axis == Axis.Row;
        sheet.Cells.SetValue(rows ? 2 : 0, rows ? 0 : 2, 5);
        sheet.Cells.SetFormula(rows ? 1 : 0, rows ? 0 : 1, rows ? "=SUM(3:3)" : "=SUM(C:C)");

        Assert.That(rows ? sheet.Rows.Move(1, 2, 7) : sheet.Columns.Move(1, 2, 7), Is.True);
        Assert.That(sheet.Cells.GetFormulaString(rows ? 5 : 0, rows ? 0 : 5),
            Is.EqualTo(rows ? "=SUM(7:7)" : "=SUM(G:G)"));
        Assert.That(sheet.Cells.GetValue(rows ? 5 : 0, rows ? 0 : 5), Is.EqualTo(5));

        sheet.Commands.Undo();
        Assert.That(sheet.Cells.GetFormulaString(rows ? 1 : 0, rows ? 0 : 1),
            Is.EqualTo(rows ? "=SUM(3:3)" : "=SUM(C:C)"));
        sheet.Commands.Redo();
        Assert.That(sheet.Cells.GetValue(rows ? 5 : 0, rows ? 0 : 5), Is.EqualTo(5));
    }

    [TestCase(Axis.Col, true)]
    [TestCase(Axis.Col, false)]
    [TestCase(Axis.Row, true)]
    [TestCase(Axis.Row, false)]
    public void Other_Sheet_Conditional_References_Follow_Shifts_And_Undo(Axis axis, bool insert)
    {
        var workbook = new Workbook();
        var source = workbook.AddSheet("Source", 10, 10);
        var target = workbook.AddSheet("Target", 10, 10);
        var row = axis == Axis.Row ? 2 : 1;
        var col = axis == Axis.Col ? 2 : 1;
        var before = axis == Axis.Col ? "=Source!C2>1" : "=Source!B3>1";
        var after = insert ? axis == Axis.Col ? "=Source!D2>1" : "=Source!B4>1" : "=Source!B2>1";
        source.Cells.SetValue(row, col, 5);
        var format = new FormulaConditionalFormat(before, new CellFormat { BackgroundColor = "green" });
        target.ConditionalFormats.Apply(new Region(0, 0), format);

        Assert.That(insert ? source.InsertCells(new Region(1, 1), axis)
            : source.RemoveCells(new Region(1, 1), axis), Is.True);
        Assert.That(format.Formula, Is.EqualTo(after));
        Assert.That(target.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor, Is.EqualTo("green"));
        source.Commands.Undo();
        Assert.That(format.Formula, Is.EqualTo(before));
        source.Commands.Redo();
        Assert.That(format.Formula, Is.EqualTo(after));
    }

    [TestCase(Axis.Col, true, false)]
    [TestCase(Axis.Col, false, false)]
    [TestCase(Axis.Row, true, false)]
    [TestCase(Axis.Row, false, false)]
    [TestCase(Axis.Col, true, true)]
    [TestCase(Axis.Col, false, true)]
    [TestCase(Axis.Row, true, true)]
    [TestCase(Axis.Row, false, true)]
    public void Relative_Conditional_References_Follow_Only_The_Shifted_Band(Axis axis, bool insert, bool foreign)
    {
        var workbook = new Workbook();
        var source = workbook.AddSheet("Source", 10, 10);
        var target = foreign ? workbook.AddSheet("Target", 10, 10) : source;
        var horizontal = axis == Axis.Col;
        for (var index = 0; index < 3; index++)
            source.Cells.SetValue(horizontal ? index : 2, horizontal ? 2 : index, 5);
        var formula = "=" + (foreign ? "Source!" : "") + (horizontal ? "C1>1" : "A3>1");
        var format = new FormulaConditionalFormat(formula, new CellFormat { BackgroundColor = "green" });
        target.ConditionalFormats.Apply(horizontal ? new Region(0, 2, 0, 0) : new Region(0, 0, 0, 2), format);

        Assert.That(insert ? source.InsertCells(new Region(1, 1), axis)
            : source.RemoveCells(new Region(1, 1), axis), Is.True);
        for (var index = 0; index < 3; index++)
            Assert.That(target.ConditionalFormats.GetFormatResult(horizontal ? index : 0, horizontal ? 0 : index)
                ?.BackgroundColor, Is.EqualTo("green"));

        source.Commands.Undo();
        Assert.That(target.ConditionalFormats.GetAllFormats().Count(), Is.EqualTo(1));
        Assert.That(format.Formula, Is.EqualTo(formula));
        source.Commands.Redo();
        for (var index = 0; index < 3; index++)
            Assert.That(target.ConditionalFormats.GetFormatResult(horizontal ? index : 0, horizontal ? 0 : index)
                ?.BackgroundColor, Is.EqualTo("green"));
    }
}
