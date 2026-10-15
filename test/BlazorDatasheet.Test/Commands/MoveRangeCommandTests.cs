using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class MoveRangeCommandTests
{
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(20, 20);
    }

    private bool Move(string from, int toRow, int toCol, Sheet? target = null)
    {
        target ??= _sheet;
        return target.Commands.ExecuteCommand(new MoveRangeCommand(_sheet.Range(from)!, target.Range(toRow, toCol)));
    }

    [Test]
    public void Move_Moves_Values_Formats_Merges_Types_And_Clears_Source()
    {
        _sheet.Cells.SetValue(0, 0, "a");
        _sheet.Cells.SetValue(1, 0, "b");
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _sheet.Cells.Merge(new Region(2, 3, 0, 0));
        _sheet.Cells.SetType(new Region(1, 0), "boolean");

        Move("A1:A4", 0, 4).Should().BeTrue();

        _sheet.Cells.GetValue(0, 4).Should().Be("a");
        _sheet.Cells.GetValue(1, 4).Should().Be("b");
        _sheet.GetFormat(0, 4).BackgroundColor.Should().Be("red");
        _sheet.Cells.GetMerge(2, 4).Should().BeEquivalentTo(new Region(2, 3, 4, 4));
        _sheet.Cells.GetCellType(1, 4).Should().Be("boolean");

        _sheet.Cells.GetValue(0, 0).Should().BeNull();
        _sheet.Cells.GetValue(1, 0).Should().BeNull();
        _sheet.GetFormat(0, 0).BackgroundColor.Should().BeNull();
        _sheet.Cells.GetMerge(2, 0).Should().BeNull();
        _sheet.Cells.GetCellType(1, 0).Should().Be("default");
    }

    [Test]
    public void Dependent_Outside_Block_Is_Rewritten_And_Recalculates()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetFormula(0, 2, "=A1+A2");

        Move("A1:A2", 0, 4);

        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=E1+E2");
        _sheet.Cells.GetValue(0, 2).Should().Be(3);
        _sheet.Cells.SetValue(0, 4, 10);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);
    }

    [Test]
    public void Formula_Inside_Block_Referencing_Inside_Moves_With_It()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetFormula(1, 0, "=A1*2");

        Move("A1:A2", 0, 4);

        _sheet.Cells.GetFormulaString(1, 4).Should().Be("=E1*2");
        _sheet.Cells.GetValue(1, 4).Should().Be(2);
        _sheet.Cells.HasFormula(1, 0).Should().BeFalse();
    }

    [Test]
    public void Formula_Inside_Block_Referencing_Outside_Is_Unchanged()
    {
        _sheet.Cells.SetValue(0, 1, 5);
        _sheet.Cells.SetFormula(0, 0, "=B1");

        Move("A1", 4, 4);

        _sheet.Cells.GetFormulaString(4, 4).Should().Be("=B1");
        _sheet.Cells.GetValue(4, 4).Should().Be(5);
        _sheet.Cells.SetValue(0, 1, 6);
        _sheet.Cells.GetValue(4, 4).Should().Be(6);
    }

    [Test]
    public void Partially_Overlapping_Reference_Is_Unchanged()
    {
        _sheet.Cells.SetFormula(0, 2, "=sum(A1:A3)");
        Move("A1:A2", 0, 4);
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=sum(A1:A3)");
    }

    [Test]
    public void Fixed_References_Move()
    {
        _sheet.Cells.SetValue(0, 0, 3);
        _sheet.Cells.SetFormula(0, 2, "=$A$1");
        Move("A1", 4, 4);
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=$E$5");
        _sheet.Cells.GetValue(0, 2).Should().Be(3);
    }

    [Test]
    public void Move_Over_Own_Source_Shifts_Values()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetValue(2, 0, 3);
        _sheet.Cells.SetFormula(1, 1, "=A2");

        Move("A1:A2", 1, 0);

        _sheet.Cells.GetValue(0, 0).Should().BeNull();
        _sheet.Cells.GetValue(1, 0).Should().Be(1);
        _sheet.Cells.GetValue(2, 0).Should().Be(2);
        _sheet.Cells.GetFormulaString(1, 1).Should().Be("=A3");

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
        _sheet.Cells.GetValue(1, 0).Should().Be(2);
        _sheet.Cells.GetValue(2, 0).Should().Be(3);
        _sheet.Cells.GetFormulaString(1, 1).Should().Be("=A2");
    }

    [Test]
    public void Move_Across_Sheets_Qualifies_References()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Sheet1", 10, 10);
        var s2 = wb.AddSheet("Sheet2", 10, 10);
        s1.Cells.SetValue(0, 0, 1); // A1
        s1.Cells.SetValue(0, 1, 4); // B1
        s1.Cells.SetFormula(1, 0, "=B1"); // A2, inside block, references outside
        s1.Cells.SetFormula(0, 2, "=A1"); // C1, outside block, references inside

        s2.Commands.ExecuteCommand(new MoveRangeCommand(s1.Range("A1:A2")!, s2.Range(1, 1))).Should().BeTrue();

        s2.Cells.GetValue(1, 1).Should().Be(1);
        s2.Cells.GetFormulaString(2, 1).Should().Be("=Sheet1!B1");
        s2.Cells.GetValue(2, 1).Should().Be(4);
        s1.Cells.GetFormulaString(0, 2).Should().Be("=Sheet2!B2");
        s1.Cells.GetValue(0, 2).Should().Be(1);
        s1.Cells.GetValue(0, 0).Should().BeNull();
        s1.Cells.HasFormula(1, 0).Should().BeFalse();

        s2.Cells.SetValue(1, 1, 7);
        s1.Cells.GetValue(0, 2).Should().Be(7);

        s2.Commands.Undo(); // the value set
        s2.Commands.Undo(); // the move
        s1.Cells.GetValue(0, 0).Should().Be(1);
        s1.Cells.GetFormulaString(1, 0).Should().Be("=B1");
        s1.Cells.GetFormulaString(0, 2).Should().Be("=A1");
        s2.Cells.GetValue(1, 1).Should().BeNull();
        s2.Cells.HasFormula(2, 1).Should().BeFalse();
        s1.Cells.SetValue(0, 0, 9);
        s1.Cells.GetValue(0, 2).Should().Be(9);
    }

    [Test]
    public void Undo_Restores_Source_Target_Dependents_And_Graph_In_One_Step()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Cells.SetFormula(0, 2, "=A1+A2");
        _sheet.Cells.SetValue(0, 4, "old");
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _sheet.Cells.Merge(new Region(0, 1, 1, 1)); // B1:B2

        Move("A1:B2", 0, 4);
        _sheet.Commands.Undo();

        _sheet.Cells.GetValue(0, 0).Should().Be(1);
        _sheet.Cells.GetValue(1, 0).Should().Be(2);
        _sheet.Cells.GetValue(0, 4).Should().Be("old");
        _sheet.Cells.GetValue(1, 4).Should().BeNull();
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=A1+A2");
        _sheet.GetFormat(0, 0).BackgroundColor.Should().Be("red");
        _sheet.GetFormat(0, 4).BackgroundColor.Should().BeNull();
        _sheet.Cells.GetMerge(0, 1).Should().BeEquivalentTo(new Region(0, 1, 1, 1));
        _sheet.Cells.GetMerge(0, 5).Should().BeNull();

        _sheet.Cells.SetValue(0, 0, 10);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);

        Move("A1:B2", 0, 4);
        _sheet.Cells.GetFormulaString(0, 2).Should().Be("=E1+E2");
        _sheet.Cells.GetValue(0, 4).Should().Be(10);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);
    }

    [Test]
    public void Move_To_Same_Place_Does_Nothing()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        Move("A1:A2", 0, 0).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }

    [Test]
    public void Move_Refuses_ReadOnly_Target()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.SetFormat(new Region(5, 5), new CellFormat { IsReadOnly = true });
        Move("A1", 5, 5).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }

    [Test]
    public void Move_Refuses_Locked_Cells_When_Protected()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.SetFormat(new Region(0, 0), new CellFormat { IsLocked = true });
        _sheet.SetFormat(new Region(5, 5), new CellFormat { IsLocked = false });
        _sheet.Protection.Protect();
        Move("A1", 5, 5).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be(1);
    }

    [Test]
    public void Move_Does_Not_Change_The_Callers_Options()
    {
        var options = new CopyOptions();
        _sheet.Commands.ExecuteCommand(new MoveRangeCommand(_sheet.Range("A1")!, _sheet.Range(2, 2), options));
        options.FormulaReferences.Should().Be(ReferenceAdjustment.Relative);
    }

    [Test]
    public void Move_Refuses_A_Target_That_Cuts_Through_A_Merge_But_A_Merge_May_Move_Over_Itself()
    {
        _sheet.Cells.Merge(new Region(5, 5, 5, 7));
        Move("A1:B1", 5, 6).Should().BeFalse();
        _sheet.Cells.GetMerge(5, 5).Should().BeEquivalentTo(new Region(5, 5, 5, 7));

        Move("F6:H6", 5, 6).Should().BeTrue();
        _sheet.Cells.GetMerge(5, 6).Should().BeEquivalentTo(new Region(5, 5, 6, 8));
    }

    [Test]
    public void Move_Carries_Validators_Metadata_And_Conditional_Formats()
    {
        var validator = new NumberValidator(true);
        var cf = new ConditionalFormat((_, _) => true, _ => new CellFormat { BackgroundColor = "green" });
        _sheet.Cells.SetValue(0, 0, 7);
        _sheet.Validators.Add(new Region(0, 0), validator);
        _sheet.Cells.SetCellMetaData(0, 0, "m", "x");
        _sheet.ConditionalFormats.Apply(new Region(0, 0), cf);
        // what the block lands on is replaced
        _sheet.Validators.Add(new Region(0, 4), new SourceValidator(new List<string> { "a" }, true));
        _sheet.Cells.SetCellMetaData(0, 4, "m", "old");

        Move("A1", 0, 4).Should().BeTrue();

        _sheet.Validators.Get(0, 4).Should().Equal(validator);
        _sheet.Validators.Get(0, 0).Should().BeEmpty();
        _sheet.Cells.GetMetaData(0, 4, "m").Should().Be("x");
        _sheet.Cells.GetMetaData(0, 0, "m").Should().BeNull();
        _sheet.ConditionalFormats.GetFormatResult(0, 4)!.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(0, 0).Should().BeNull();

        _sheet.Commands.Undo();

        _sheet.Validators.Get(0, 0).Should().Equal(validator);
        _sheet.Validators.Get(0, 4).Should().ContainSingle().Which.Should().BeOfType<SourceValidator>();
        _sheet.Cells.GetMetaData(0, 0, "m").Should().Be("x");
        _sheet.Cells.GetMetaData(0, 4, "m").Should().Be("old");
        _sheet.ConditionalFormats.GetFormatResult(0, 0)!.BackgroundColor.Should().Be("green");
        _sheet.ConditionalFormats.GetFormatResult(0, 4).Should().BeNull();
    }

    [Test]
    public void Move_Splits_Validators_And_Metadata_Partly_In_The_Block()
    {
        var validator = new NumberValidator(true);
        _sheet.Validators.Add(new Region(0, 2, 0, 0), validator);
        _sheet.Cells.SetCellMetaData(new Region(0, 2, 0, 0), "m", "x");

        Move("A2", 5, 5).Should().BeTrue();

        _sheet.Validators.Get(0, 0).Should().Equal(validator);
        _sheet.Validators.Get(1, 0).Should().BeEmpty();
        _sheet.Validators.Get(2, 0).Should().Equal(validator);
        _sheet.Validators.Get(5, 5).Should().Equal(validator);
        _sheet.Cells.GetMetaData(1, 0, "m").Should().BeNull();
        _sheet.Cells.GetMetaData(5, 5, "m").Should().Be("x");

        _sheet.Commands.Undo();
        _sheet.Validators.Get(1, 0).Should().Equal(validator);
        _sheet.Validators.Get(5, 5).Should().BeEmpty();
        _sheet.Cells.GetMetaData(1, 0, "m").Should().Be("x");
        _sheet.Cells.GetMetaData(5, 5, "m").Should().BeNull();
    }

    [Test]
    public void Move_Within_Overlapping_Regions_Carries_Validators()
    {
        var validator = new NumberValidator(true);
        _sheet.Validators.Add(new Region(0, 0), validator);

        Move("A1:B1", 0, 1).Should().BeTrue();

        _sheet.Validators.Get(0, 0).Should().BeEmpty();
        _sheet.Validators.Get(0, 1).Should().Equal(validator);
        _sheet.Validators.Get(0, 2).Should().BeEmpty();
        _sheet.Commands.Undo();
        _sheet.Validators.Get(0, 0).Should().Equal(validator);
        _sheet.Validators.Get(0, 1).Should().BeEmpty();
    }

    [Test]
    public void Move_To_Another_Sheet_Carries_Validators_And_Metadata()
    {
        var other = _sheet.Workbook.AddSheet("Other", 20, 20);
        var validator = new NumberValidator(true);
        _sheet.Validators.Add(new Region(0, 0), validator);
        _sheet.Cells.SetCellMetaData(0, 0, "m", "x");

        Move("A1", 2, 2, other).Should().BeTrue();

        other.Validators.Get(2, 2).Should().Equal(validator);
        other.Cells.GetMetaData(2, 2, "m").Should().Be("x");
        _sheet.Validators.Get(0, 0).Should().BeEmpty();
        _sheet.Cells.GetMetaData(0, 0, "m").Should().BeNull();

        other.Commands.Undo();
        other.Validators.Get(2, 2).Should().BeEmpty();
        other.Cells.GetMetaData(2, 2, "m").Should().BeNull();
        _sheet.Validators.Get(0, 0).Should().Equal(validator);
        _sheet.Cells.GetMetaData(0, 0, "m").Should().Be("x");
    }

    [Test]
    public void Move_Of_Values_Only_Leaves_Validators()
    {
        var validator = new NumberValidator(true);
        _sheet.Cells.SetValue(0, 0, 7);
        _sheet.Validators.Add(new Region(0, 0), validator);

        _sheet.Commands.ExecuteCommand(new MoveRangeCommand(_sheet.Range("A1")!, _sheet.Range(0, 4),
            CopyOptions.ValuesOnly)).Should().BeTrue();

        _sheet.Cells.GetValue(0, 4).Should().Be(7);
        _sheet.Validators.Get(0, 0).Should().Equal(validator);
        _sheet.Validators.Get(0, 4).Should().BeEmpty();
    }

    [Test]
    public void Move_Rewrites_Named_Ranges_Into_The_Block()
    {
        _sheet.Cells.SetValue(0, 0, 7);
        _sheet.NamedRanges.Set("inside", "A1");
        _sheet.NamedRanges.Set("partly", "A1:A2");
        _sheet.Cells.SetFormula(10, 10, "=inside");

        Move("A1", 0, 4).Should().BeTrue();

        _sheet.NamedRanges.GetRangeString("inside").Should().Be("Sheet1!E1");
        _sheet.NamedRanges.GetRangeString("partly").Should().Be("Sheet1!A1:A2");
        _sheet.Cells.GetValue(10, 10).Should().Be(7);

        _sheet.Commands.Undo();
        _sheet.NamedRanges.GetRangeString("inside").Should().Be("Sheet1!A1");
        _sheet.Cells.GetValue(10, 10).Should().Be(7);
    }

    [Test]
    public void Move_Rewrites_Conditional_Format_Formulas_On_Every_Sheet()
    {
        var other = _sheet.Workbook.AddSheet("Other", 20, 20);
        _sheet.Cells.SetValue(0, 0, 7);
        var here = new FormulaConditionalFormat("=A1>5", new CellFormat { BackgroundColor = "green" });
        var there = new FormulaConditionalFormat("=Sheet1!A1>5", new CellFormat { BackgroundColor = "green" });
        _sheet.ConditionalFormats.Apply(new Region(5, 5), here);
        other.ConditionalFormats.Apply(new Region(5, 5), there);

        Move("A1", 0, 4).Should().BeTrue();

        here.Formula.Should().Be("=E1>5");
        there.Formula.Should().Be("=Sheet1!E1>5");
        _sheet.ConditionalFormats.GetFormatResult(5, 5)!.BackgroundColor.Should().Be("green");
        other.ConditionalFormats.GetFormatResult(5, 5)!.BackgroundColor.Should().Be("green");

        _sheet.Commands.Undo();
        here.Formula.Should().Be("=A1>5");
        there.Formula.Should().Be("=Sheet1!A1>5");
    }

    [Test]
    public void Move_Leaves_Relative_Conditional_Format_Formulas_Reading_Past_The_Block()
    {
        // applied to A1:A3, the rule reads A1:A3, so moving A1 alone leaves it alone.
        var cf = new FormulaConditionalFormat("=A1>5", new CellFormat { BackgroundColor = "green" });
        _sheet.ConditionalFormats.Apply(new Region(0, 2, 0, 0), cf);
        var absolute = new FormulaConditionalFormat("=$A$1>5", new CellFormat { BackgroundColor = "green" });
        _sheet.ConditionalFormats.Apply(new Region(0, 2, 2, 2), absolute);

        Move("A1", 0, 4).Should().BeTrue();

        cf.Formula.Should().Be("=A1>5");
        absolute.Formula.Should().Be("=$E$1>5");
    }
}
