using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;
using System.Linq;

namespace BlazorDatasheet.Test.Commands;

public class CopyRangeCommandTests
{
    private Sheet _sheet;

    [SetUp]
    public void Setup_Sheet()
    {
        _sheet = new Sheet(100, 100);
    }

    [Test]
    public void Copy_Region_Copies_Values()
    {
        _sheet.Range("A1:B2").Value = "test";
        _sheet.Range("C1:D2").Value = "prev";
        var cmd = new CopyRangeCommand(_sheet.Range("A1:B2")!, _sheet.Range("C1:D2"), CopyOptions.DefaultCopyOptions);

        cmd.Execute(_sheet);
        for (int r = 0; r < 2; r++)
        {
            _sheet.Cells[r, 2].Value.Should().Be("test");
            _sheet.Cells[r, 3].Value.Should().Be("test");
        }

        cmd.Undo(_sheet);

        for (int r = 0; r < 2; r++)
        {
            _sheet.Cells[r, 2].Value.Should().Be("prev");
            _sheet.Cells[r, 3].Value.Should().Be("prev");
        }
    }

    [Test]
    public void Single_Cell_Copy_Copies_Values()
    {
        _sheet.Range(1, 1).Value = "test";
        _sheet.Cells.GetValue(1, 2).Should().BeNull();
        var cmd = new CopyRangeCommand(_sheet.Range(1, 1), _sheet.Range(1, 2), CopyOptions.DefaultCopyOptions);
        cmd.Execute(_sheet);
        _sheet.Cells.GetValue(1, 2).Should().Be("test");
        cmd.Undo(_sheet);
        _sheet.Cells.GetValue(0, 1).Should().BeNull();
    }

    [Test]
    public void Copy_Formula_Moves_References()
    {
        _sheet.Cells[1, 1].Formula = "=sum(D1:E2)";

        var copyCmd = new CopyRangeCommand(_sheet.Range(1, 1), _sheet.Range(3, 4), CopyOptions.DefaultCopyOptions);
        copyCmd.Execute(_sheet);
        var cellCopied = _sheet.Cells[3, 4];
        cellCopied.HasFormula().Should().Be(true);
        cellCopied.Formula.Should().Be("=sum(G3:H4)");
        _sheet.Cells[1, 1].Formula.Should().Be("=sum(D1:E2)");

        copyCmd.Undo(_sheet);
        _sheet.Cells[3, 4].HasFormula().Should().Be(false);
        _sheet.Cells.GetValue(3, 4).Should().BeNull();
    }

    [Test]
    public void Copy_Formula_DoesntMove_Fixed_References()
    {
        _sheet.Cells[1, 1].Formula = "=sum($D$1:$E$2)";
        var copyCmd = new CopyRangeCommand(_sheet.Range(1, 1), _sheet.Range(3, 4), CopyOptions.DefaultCopyOptions);
        copyCmd.Execute(_sheet);
        var cellCopied = _sheet.Cells[3, 4];
        cellCopied.HasFormula().Should().Be(true);
        cellCopied.Formula.Should().Be("=sum($D$1:$E$2)");
        _sheet.Cells[1, 1].Formula.Should().Be("=sum($D$1:$E$2)");

        copyCmd.Undo(_sheet);
        _sheet.Cells[3, 4].HasFormula().Should().Be(false);
    }

    [Test]
    public void Copy_Format_With_Row_Col_Format_Copies_And_Undos()
    {
        _sheet.SetFormat(new ColumnRegion(1), new CellFormat() { BackgroundColor = "col" });
        _sheet.SetFormat(new RowRegion(1), new CellFormat() { BackgroundColor = "row" });

        var colCopy = new CopyRangeCommand(_sheet.Range(0, 1), _sheet.Range(0, 2), CopyOptions.DefaultCopyOptions);
        var rowCopy = new CopyRangeCommand(_sheet.Range(1, 0), _sheet.Range(2, 0), CopyOptions.DefaultCopyOptions);
        var intersectCopy =
            new CopyRangeCommand(_sheet.Range(1, 1), _sheet.Range(2, 2), CopyOptions.DefaultCopyOptions);

        colCopy.Execute(_sheet);
        rowCopy.Execute(_sheet);
        intersectCopy.Execute(_sheet);

        _sheet.Cells[0, 2].Format!.BackgroundColor.Should().Be("col");
        _sheet.Cells[2, 0].Format!.BackgroundColor.Should().Be("row");
        _sheet.Cells[2, 2].Format!.BackgroundColor.Should().Be("row");
    }

    [Test]
    public void Copy_Merges_Copies_And_Undo_Restores_Previous_Merges()
    {
        _sheet.Cells.Merge(new Region(0, 1, 0, 1)); // A1:B2
        _sheet.Cells.Merge(new Region(5, 5, 5, 6)); // F6:G6 - will be overwritten at target
        var cmd = new CopyRangeCommand(_sheet.Range("A1:B2"), _sheet.Range(5, 5), CopyOptions.DefaultCopyOptions);
        cmd.Execute(_sheet).Should().BeTrue();

        _sheet.Cells.GetMerge(5, 5).Should().BeEquivalentTo(new Region(5, 6, 5, 6));
        _sheet.Cells.GetMerge(0, 0).Should().BeEquivalentTo(new Region(0, 1, 0, 1));

        cmd.Undo(_sheet);
        _sheet.Cells.GetMerge(5, 5).Should().BeEquivalentTo(new Region(5, 5, 5, 6));
        _sheet.Cells.GetMerge(6, 5).Should().BeNull();
    }

    [Test]
    public void Copy_Merges_Skips_Merges_Partially_Outside_Source()
    {
        _sheet.Cells.Merge(new Region(0, 2, 0, 0)); // A1:A3
        var cmd = new CopyRangeCommand(_sheet.Range("A1:A2"), _sheet.Range(0, 5), CopyOptions.DefaultCopyOptions);
        cmd.Execute(_sheet);
        _sheet.Cells.AnyMerges(new Region(0, 1, 5, 5)).Should().BeFalse();
    }

    [Test]
    public void Copy_Types_Copies_And_Undoes()
    {
        _sheet.Cells.SetType(new Region(0, 1, 0, 0), "boolean");
        _sheet.Cells.SetType(new Region(0, 0, 3, 3), "text");
        var cmd = new CopyRangeCommand(_sheet.Range("A1:A2"), _sheet.Range(0, 3), CopyOptions.DefaultCopyOptions);
        cmd.Execute(_sheet);
        _sheet.Cells.GetCellType(0, 3).Should().Be("boolean");
        _sheet.Cells.GetCellType(1, 3).Should().Be("boolean");
        cmd.Undo(_sheet);
        _sheet.Cells.GetCellType(0, 3).Should().Be("text");
        _sheet.Cells.GetCellType(1, 3).Should().Be("default");
    }

    [Test]
    public void Copy_Value_Over_Formula_Removes_It_From_Dependency_Graph()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetFormula(0, 1, "=A1"); // B1
        _sheet.Cells.SetValue(0, 2, 5); // C1
        _sheet.Cells.GetValue(0, 1).Should().Be(1);

        var cmd = new CopyRangeCommand(_sheet.Range(0, 2), _sheet.Range(0, 1), CopyOptions.DefaultCopyOptions);
        _sheet.Commands.ExecuteCommand(cmd);
        _sheet.Cells.HasFormula(0, 1).Should().BeFalse();
        _sheet.FormulaEngine.DependencyManager.HasDependents(0, 0, _sheet.Name).Should().BeFalse();

        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.Cells.GetValue(0, 1).Should().Be(5);

        _sheet.Commands.Undo();
        _sheet.Commands.Undo();
        _sheet.Cells.HasFormula(0, 1).Should().BeTrue();
        _sheet.Cells.SetValue(0, 0, 3);
        _sheet.Cells.GetValue(0, 1).Should().Be(3);
    }

    [Test]
    public void Copy_Formula_On_Renamed_Sheet_Keeps_References_On_That_Sheet()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Data", 10, 10);
        sheet.Cells.SetValue(0, 0, 7);
        sheet.Cells.SetFormula(0, 1, "=A1");
        var cmd = new CopyRangeCommand(sheet.Range(0, 1), sheet.Range(1, 1), CopyOptions.DefaultCopyOptions);
        sheet.Commands.ExecuteCommand(cmd);
        sheet.Cells.GetFormula(1, 1)!.References.First().SheetName.Should().Be("Data");
        sheet.Cells.GetFormulaString(1, 1).Should().Be("=A2");
    }

    [Test]
    public void Copy_Across_Sheets_Rebinds_Implicit_References_To_Target_Sheet()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Sheet1", 10, 10);
        var s2 = wb.AddSheet("Sheet2", 10, 10);
        s1.Cells.SetValue(0, 0, 1);
        s2.Cells.SetValue(0, 0, 10);
        s1.Cells.SetFormula(0, 1, "=A1+Sheet1!A1");
        s1.Cells.Merge(new Region(2, 3, 1, 1));
        s1.SetFormat(new Region(0, 3, 1, 1), new CellFormat { BackgroundColor = "red" });

        var cmd = new CopyRangeCommand(s1.Range(0, 3, 1, 1), s2.Range(0, 1), CopyOptions.DefaultCopyOptions);
        s2.Commands.ExecuteCommand(cmd).Should().BeTrue();

        s2.Cells.GetFormulaString(0, 1).Should().Be("=A1+Sheet1!A1");
        s2.Cells.GetFormula(0, 1)!.References.First().SheetName.Should().Be("Sheet2");
        s2.Cells.GetValue(0, 1).Should().Be(11);
        s2.Cells.GetMerge(2, 1).Should().BeEquivalentTo(new Region(2, 3, 1, 1));
        s2.GetFormat(3, 1).BackgroundColor.Should().Be("red");
        s1.Cells.GetValue(0, 1).Should().Be(2);

        s2.Commands.Undo();
        s2.Cells.HasFormula(0, 1).Should().BeFalse();
        s2.Cells.GetMerge(2, 1).Should().BeNull();
        s2.GetFormat(3, 1).BackgroundColor.Should().BeNull();
    }
}
