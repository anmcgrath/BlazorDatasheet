using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class PasteSnapshotCommandTests
{
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(10, 10);
    }

    private static ClipboardSnapshot Snapshot()
    {
        var s = new ClipboardSnapshot(2, 2) { Origin = new CellPosition(0, 0) };
        s[0, 0] = new ClipboardCell { RawText = "12", Format = new CellFormat { FontWeight = "bold" } };
        s[0, 1] = new ClipboardCell { Value = CellValue.Text("t"), Format = new CellFormat { FontWeight = "bold" } };
        s[1, 0] = new ClipboardCell { Formula = "=A1*2" };
        s[1, 1] = new ClipboardCell { RawText = "x", Type = "text" };
        return s;
    }

    [Test]
    public void Pastes_Values_Formulas_Types_Formats_And_Merges_In_One_Undo_Step()
    {
        _sheet.Cells.SetValue(2, 2, "old");
        var snapshot = Snapshot();
        snapshot.Merges.Add(new Region(0, 0, 0, 1));
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(snapshot, new CellPosition(2, 2))).Should().BeTrue();

        _sheet.Cells.GetValue(2, 2).Should().Be(12);
        _sheet.Cells.GetValue(2, 3).Should().BeNull("the merge keeps only the anchor value");
        _sheet.Cells.GetFormulaString(3, 2).Should().Be("=C3*2");
        _sheet.Cells.GetValue(3, 2).Should().Be(24);
        _sheet.Cells.GetCellType(3, 3).Should().Be("text");
        _sheet.Cells.GetValue(3, 3).Should().Be("x");
        _sheet.GetFormat(2, 2).FontWeight.Should().Be("bold");
        _sheet.Cells.GetMerge(2, 2).Should().BeEquivalentTo(new Region(2, 2, 2, 3));

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(2, 2).Should().Be("old");
        _sheet.Cells.HasFormula(3, 2).Should().BeFalse();
        _sheet.Cells.GetCellType(3, 3).Should().Be("default");
        _sheet.GetFormat(2, 2).FontWeight.Should().BeNull();
        _sheet.Cells.GetMerge(2, 2).Should().BeNull();
    }

    [Test]
    public void Values_Mode_Skips_Formats_Formulas_And_Types()
    {
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(Snapshot(), new CellPosition(0, 0), PasteMode.Values));
        _sheet.Cells.GetValue(0, 0).Should().Be(12);
        _sheet.Cells.HasFormula(1, 0).Should().BeFalse();
        _sheet.Cells.GetValue(1, 0).Should().BeNull();
        _sheet.GetFormat(0, 0).FontWeight.Should().BeNull();
        _sheet.Cells.GetCellType(1, 1).Should().Be("default");
    }

    [Test]
    public void Formats_Mode_Leaves_Values_Alone()
    {
        _sheet.Cells.SetValue(0, 0, "keep");
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(Snapshot(), new CellPosition(0, 0), PasteMode.Formats));
        _sheet.Cells.GetValue(0, 0).Should().Be("keep");
        _sheet.GetFormat(0, 0).FontWeight.Should().Be("bold");
    }

    [Test]
    public void Raw_Text_Respects_Target_Cell_Type()
    {
        _sheet.Cells.SetType(new Region(0, 0), "text");
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(Snapshot(), new CellPosition(0, 0), PasteMode.Values));
        _sheet.Cells.GetValue(0, 0).Should().Be("12");
    }

    [Test]
    public void Empty_Snapshot_Cells_Clear_Target_Cells()
    {
        _sheet.Cells.SetValue(0, 1, "old");
        var s = new ClipboardSnapshot(1, 2);
        s[0, 0] = new ClipboardCell { RawText = "a" };
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(s, new CellPosition(0, 0)));
        _sheet.Cells.GetValue(0, 1).Should().BeNull();
    }

    [Test]
    public void Tiles_And_Clips_To_Sheet()
    {
        var origins = new[] { new CellPosition(0, 0), new CellPosition(0, 2), new CellPosition(9, 9) };
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(Snapshot(), origins)).Should().BeTrue();
        _sheet.Cells.GetValue(0, 0).Should().Be(12);
        _sheet.Cells.GetValue(0, 2).Should().Be(12);
        _sheet.Cells.GetValue(9, 9).Should().Be(12);
        _sheet.Cells.GetFormulaString(1, 2).Should().Be("=C1*2");
    }

    [Test]
    public void Formula_Without_Origin_Is_Pasted_Verbatim_And_Fixed_References_Stay()
    {
        var s = new ClipboardSnapshot(1, 2);
        s[0, 0] = new ClipboardCell { Formula = "=A1" };
        s[0, 1] = new ClipboardCell { Formula = "=$A$1" };
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(s, new CellPosition(3, 3)));
        _sheet.Cells.GetFormulaString(3, 3).Should().Be("=A1");

        s.Origin = new CellPosition(0, 0);
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(s, new CellPosition(5, 5)));
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=F6");
        _sheet.Cells.GetFormulaString(5, 6).Should().Be("=$A$1");
    }

    [Test]
    public void Invalid_Formula_Is_Pasted_As_Text()
    {
        var s = new ClipboardSnapshot(1, 1);
        s[0, 0] = new ClipboardCell { Formula = "=SUM(" };
        _sheet.Commands.ExecuteCommand(new PasteSnapshotCommand(s, new CellPosition(0, 0)));
        _sheet.Cells.HasFormula(0, 0).Should().BeFalse();
        _sheet.Cells.GetValue(0, 0).Should().Be("=SUM(");
    }

    [Test]
    public void Clipboard_PasteText_And_PasteSnapshot_Report_Region_And_Tile()
    {
        var cb = _sheet.Workbook.Clipboard;
        var region = cb.PasteText(_sheet, new Region(0, 3, 0, 0), "1\n2");
        region.Should().BeEquivalentTo(new Region(0, 3, 0, 0));
        _sheet.Cells.GetValue(3, 0).Should().Be(2);

        cb.PasteSnapshot(_sheet, new Region(5, 5), new ClipboardSnapshot(0, 0)).Should().BeNull();
    }
}
