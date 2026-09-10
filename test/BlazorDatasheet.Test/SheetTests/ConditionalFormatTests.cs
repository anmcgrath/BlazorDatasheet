using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class ConditionalFormatTests
{
    private ConditionalFormatManager cm;

    /// <summary>
    /// Sheet of size 2 x 2
    /// </summary>
    private Sheet sheet;

    private ConditionalFormat greaterThanEqualToZeroRedBgCf;
    private string redBgColor = "#ff0000";

    [SetUp]
    public void Setup()
    {
        sheet = new Sheet(4, 4);
        cm = sheet.ConditionalFormats;
        greaterThanEqualToZeroRedBgCf = new ConditionalFormat(
            (posn, s) => s.Cells.GetCell(posn.row, posn.col).GetValue<int?>() >= 0
            , (cell => new CellFormat
            {
                BackgroundColor = redBgColor
            }));
    }

    [Test]
    public void Set_Cf_To_Whole_Sheet_Applies_Correctly()
    {
        sheet.Cells.SetValue(0, 0, -1);
        cm.Apply(sheet.Region, greaterThanEqualToZeroRedBgCf);
        var format = cm.GetFormatResult(0, 0);
        Assert.IsNull(format);
        sheet.Cells.SetValue(0, 0, 1);

        format = cm.GetFormatResult(0, 0);
        Assert.AreEqual(format.BackgroundColor, redBgColor);
    }

    [Test]
    public void Cf_Correctly_Passes_All_Cells_To_Func()
    {
        // Create a conditional format that sets the background color to
        // a string which is equal to the number of cells that have the conditional
        // format registered
        var cf = new ConditionalFormat(
            (posn, sheet) => true, (cell, cells) => new CellFormat() { BackgroundColor = cells.Count().ToString() });
        cf.IsShared = true;
        cm.Apply(sheet.Region, cf);
        var formatApplied = cm.GetFormatResult(0, 0);
        Assert.NotNull(formatApplied);
        Assert.AreEqual(sheet.Region.Area.ToString(), formatApplied!.BackgroundColor);
    }

    [Test]
    public void Shared_Conditional_Format_Recomputes_When_Using_Bulk_Load_Constructor()
    {
        var values = new[]
        {
            new[] { CellValue.Number(1d) },
            new[] { CellValue.Number(2d) },
            new[] { CellValue.Number(3d) }
        };
        var bulkSheet = new Sheet(3, 1, values);
        var cf = new NumberScaleConditionalFormat(Color.Red, Color.Green);

        bulkSheet.ConditionalFormats.Apply(bulkSheet.Region, cf);
        var initialBackground = bulkSheet.ConditionalFormats.GetFormatResult(1, 0)!.BackgroundColor;

        bulkSheet.Cells.SetValue(2, 0, CellValue.Number(100d));
        var updatedBackground = bulkSheet.ConditionalFormats.GetFormatResult(1, 0)!.BackgroundColor;

        Assert.AreNotEqual(initialBackground, updatedBackground);
    }

    [Test]
    public void Number_Scale_Does_Not_Clear_Foreground_Color_When_Auto_Text_Color_Is_Disabled()
    {
        sheet.Cells.SetValue(0, 0, 1);
        var foregroundCf = new ConditionalFormat((_, _) => true,
            _ => new CellFormat { ForegroundColor = "rgb(10,20,30)" });
        var numberScaleCf = new NumberScaleConditionalFormat(Color.Red, Color.Green);

        sheet.ConditionalFormats.Apply(new Region(0, 0), foregroundCf);
        sheet.ConditionalFormats.Apply(new Region(0, 0), numberScaleCf);

        var format = sheet.ConditionalFormats.GetFormatResult(0, 0);

        Assert.NotNull(format!.BackgroundColor);
        Assert.AreEqual("rgb(10,20,30)", format.ForegroundColor);
    }

    [Test]
    public void Number_Scale_Sets_Foreground_Color_When_Auto_Text_Color_Is_Enabled()
    {
        sheet.Cells.SetValue(0, 0, 1);
        var numberScaleCf = new NumberScaleConditionalFormat(Color.Red, Color.Green)
        {
            AutoTextColor = true
        };

        sheet.ConditionalFormats.Apply(new Region(0, 0), numberScaleCf);

        var format = sheet.ConditionalFormats.GetFormatResult(0, 0);

        Assert.NotNull(format!.ForegroundColor);
    }

    [Test]
    public void Conditional_Format_Shifts_When_Row_Inserted_And_Removed_Before()
    {
        // Create a new conditional format that is always run and sets the background colour to the row number
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Row.ToString() });
        // Set this format to the second row in the sheet (sheet has 2 rows)
        sheet.ConditionalFormats.Apply(new RowRegion(1), cf);
        sheet.Rows.InsertAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("2", sheet.ConditionalFormats.GetFormatResult(2, 0)?.BackgroundColor);
        sheet.Rows.RemoveAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("1", sheet.ConditionalFormats.GetFormatResult(1, 0)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Shifts_When_Col_Inserted_And_Removed_Before()
    {
        
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Col.ToString() });
        sheet.ConditionalFormats.Apply(new ColumnRegion(1), cf);
        sheet.Columns.InsertAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 1)?.BackgroundColor);
        Assert.AreEqual("2", sheet.ConditionalFormats.GetFormatResult(0, 2)?.BackgroundColor);
        sheet.Columns.RemoveAt(0);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor);
        Assert.AreEqual("1", sheet.ConditionalFormats.GetFormatResult(0, 1)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Expands_When_Col_Inserted_Inside_It()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Col.ToString() });

        sheet.ConditionalFormats.Apply(new Region(1, 2, 1, 2), cf);
        sheet.Columns.InsertAt(1);
        Assert.AreEqual("3", sheet.ConditionalFormats.GetFormatResult(1, 3)?.BackgroundColor);
        sheet.Columns.RemoveAt(2);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(1, 3)?.BackgroundColor);
    }

    [Test]
    public void Conditional_Format_Expands_When_Row_Inserted_Inside_It()
    {
        var cf = new ConditionalFormat((posn, sheet) => true,
            cell => new CellFormat() { BackgroundColor = cell.Row.ToString() });

        sheet.ConditionalFormats.Apply(new Region(1, 2, 1, 2), cf);
        sheet.Rows.InsertAt(1);
        Assert.AreEqual("3", sheet.ConditionalFormats.GetFormatResult(3, 1)?.BackgroundColor);
        sheet.Rows.RemoveAt(2);
        Assert.Null(sheet.ConditionalFormats.GetFormatResult(3, 1)?.BackgroundColor);
    }

    private const string GreenBg = "#00ff00";

    private static FormulaConditionalFormat GreenIf(string formula) =>
        new(formula, new CellFormat() { BackgroundColor = GreenBg });

    private string? Bg(int row, int col) => cm.GetFormatResult(row, col)?.BackgroundColor;

    [Test]
    public void Formula_Cf_Rebases_Relative_References_From_The_Anchor()
    {
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Cells.SetValue(1, 0, 10);
        sheet.Cells.SetValue(2, 0, 10);

        cm.Apply(new Region(0, 2, 0, 0), GreenIf("=A1>5"));

        Bg(0, 0).Should().BeNull();
        Bg(1, 0).Should().Be(GreenBg);
        Bg(2, 0).Should().Be(GreenBg);
    }

    [Test]
    public void Formula_Cf_With_Fixed_Column_Highlights_Whole_Rows()
    {
        sheet.Cells.SetValue(1, 1, 5);
        cm.Apply(new Region(0, 2, 0, 2), GreenIf("=$B1>0"));

        for (int col = 0; col <= 2; col++)
        {
            Bg(0, col).Should().BeNull();
            Bg(1, col).Should().Be(GreenBg);
            Bg(2, col).Should().BeNull();
        }
    }

    [Test]
    public void Formula_Cf_With_Fixed_Row_Highlights_Whole_Columns()
    {
        // anchor is A1, so the cell at column index 1 evaluates C$1
        sheet.Cells.SetValue(0, 2, 5);
        cm.Apply(new Region(0, 2, 0, 2), GreenIf("=B$1>0"));

        for (int row = 0; row <= 2; row++)
        {
            Bg(row, 0).Should().BeNull();
            Bg(row, 1).Should().Be(GreenBg);
            Bg(row, 2).Should().BeNull();
        }
    }

    [Test]
    public void Formula_Cf_Row_And_Column_Functions_Report_The_Target_Cell()
    {
        cm.Apply(sheet.Region, GreenIf("=POW(-1,ROW()+COLUMN())=1"));

        Bg(0, 0).Should().Be(GreenBg);
        Bg(0, 1).Should().BeNull();
        Bg(1, 0).Should().BeNull();
        Bg(1, 1).Should().Be(GreenBg);
    }

    [Test]
    public void Formula_Cf_Above_The_Anchor_Is_Ref_Error_And_Not_Applied()
    {
        var cf = GreenIf("=A1>0");
        cm.Apply(new Region(1, 2, 0, 0), cf);
        cf.Anchor.Should().Be(new CellPosition(1, 0));

        // one row above the anchor shifts A1 to row -1
        var act = () => cf.Predicate!.Invoke(new CellPosition(0, 0), sheet);
        act.Should().NotThrow();
        act().Should().BeFalse();

        var formula = sheet.FormulaEngine.ParseFormula("=A1", sheet.Name);
        sheet.FormulaEngine.EvaluateFormulaAt(formula, new CellPosition(1, 0), 0, 0, sheet.Name)
            .ValueType.Should().Be(CellValueType.Error);
    }

    [Test]
    public void Formula_Cf_Shifts_When_Row_Inserted_Before_The_Anchor()
    {
        var cf = GreenIf("=A1>0");
        cm.Apply(new Region(1, 2, 0, 0), cf);

        sheet.Rows.InsertAt(0);

        cf.Formula.Should().Be("=A2>0");
        cf.Anchor.Should().Be(new CellPosition(2, 0));
    }

    [Test]
    public void Formula_Cf_Is_Unchanged_When_Row_Inserted_Inside_The_Range()
    {
        var cf = GreenIf("=A1>0");
        cm.Apply(new Region(0, 2, 0, 0), cf);

        sheet.Rows.InsertAt(1);

        cf.Formula.Should().Be("=A1>0");
        cf.Anchor.Should().Be(new CellPosition(0, 0));
    }

    [Test]
    public void Formula_Cf_Rebases_When_The_Anchor_Row_Is_Removed()
    {
        sheet.Cells.SetValue(1, 1, 5);
        var cf = GreenIf("=$B1>0");
        cm.Apply(new Region(0, 2, 0, 0), cf);
        Bg(1, 0).Should().Be(GreenBg);

        sheet.Rows.RemoveAt(0);

        cf.Formula.Should().Be("=$B1>0");
        cf.Anchor.Should().Be(new CellPosition(0, 0));
        Bg(0, 0).Should().Be(GreenBg);
        Bg(1, 0).Should().BeNull();
    }

    [Test]
    public void Formula_Cf_Reference_Removed_Entirely_Becomes_Ref_Error()
    {
        sheet.Cells.SetValue(1, 1, 5);
        var cf = GreenIf("=$B$2>0");
        cm.Apply(new Region(0, 0, 0, 0), cf);
        Bg(0, 0).Should().Be(GreenBg);

        sheet.Rows.RemoveAt(1);

        cf.Formula.Should().Be("=#REF!>0");
        Bg(0, 0).Should().BeNull();
    }

    [Test]
    public void Formula_Cf_Undo_Restores_Formula_And_Region_After_Insert()
    {
        var cf = GreenIf("=A1>0");
        cm.Apply(new Region(1, 2, 0, 0), cf);

        sheet.Rows.InsertAt(0);
        cf.Formula.Should().Be("=A2>0");

        sheet.Commands.Undo();

        cf.Formula.Should().Be("=A1>0");
        cf.Anchor.Should().Be(new CellPosition(1, 0));
    }

    [Test]
    public void Formula_Cf_Undo_Restores_Formula_And_Region_After_Remove()
    {
        sheet.Cells.SetValue(1, 1, 5);
        var cf = GreenIf("=$B1>0");
        cm.Apply(new Region(0, 2, 0, 0), cf);

        sheet.Rows.RemoveAt(0);
        sheet.Commands.Undo();

        cf.Formula.Should().Be("=$B1>0");
        cf.Anchor.Should().Be(new CellPosition(0, 0));
        Bg(1, 0).Should().Be(GreenBg);
    }

    [Test]
    public void Formula_Cf_Marks_Applied_Region_Dirty_When_A_Precedent_Outside_It_Changes()
    {
        cm.Apply(new Region(0, 2, 0, 0), GreenIf("=$B1>0"));

        var dirtyRows = new List<Interval>();
        var colStart = int.MaxValue;
        sheet.SheetDirty += (_, e) =>
        {
            dirtyRows.AddRange(e.DirtyRows.GetAllIntervals());
            colStart = Math.Min(colStart, e.DirtyColStart);
        };

        sheet.Cells.SetValue(0, 1, 5);

        dirtyRows.SelectMany(x => Enumerable.Range(x.Start, x.Size)).Should().Contain(new[] { 0, 1, 2 });
        colStart.Should().Be(0);
    }
}
