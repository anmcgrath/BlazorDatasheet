using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

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
}