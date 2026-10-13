using System.Linq;
using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Clipboard;

public class WorkbookClipboardTests
{
    private Sheet _sheet = null!;
    private WorkbookClipboard _clipboard = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(20, 20);
        _clipboard = _sheet.Workbook.Clipboard;
    }

    private void Fill2X2()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(0, 1, 2);
        _sheet.Cells.SetValue(1, 0, 3);
        _sheet.Cells.SetValue(1, 1, 4);
    }

    [Test]
    public void Copy_Sets_Entry_And_Raises_Changed()
    {
        ClipboardChangedEventArgs? args = null;
        _clipboard.Changed += (_, e) => args = e;

        var entry = _clipboard.Copy(_sheet.Range("A1:B2"));

        _clipboard.Current.Should().BeSameAs(entry);
        _clipboard.IsActive.Should().BeTrue();
        entry.Mode.Should().Be(ClipboardMode.Copy);
        entry.Source.Region.Should().BeEquivalentTo(new Region(0, 1, 0, 1));
        args!.OldEntry.Should().BeNull();
        args.NewEntry.Should().BeSameAs(entry);

        _clipboard.Clear();
        _clipboard.Current.Should().BeNull();
        args.NewEntry.Should().BeNull();
    }

    [Test]
    public void Copy_Clips_Source_To_Sheet()
    {
        var entry = _clipboard.Copy(_sheet.Range(new ColumnRegion(1)));
        entry.Source.Region.Should().BeEquivalentTo(new Region(0, 19, 1, 1));
    }

    [Test]
    public void Entry_Is_Cleared_When_Source_Changes_But_Not_Otherwise()
    {
        _clipboard.Copy(_sheet.Range("A1:B2"));
        _sheet.Cells.SetValue(5, 5, "elsewhere");
        _clipboard.IsActive.Should().BeTrue();
        _sheet.SetFormat(new Region(5, 5), new CellFormat { BackgroundColor = "red" });
        _clipboard.IsActive.Should().BeTrue();

        _sheet.Cells.SetValue(1, 1, "inside");
        _clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public void Entry_Is_Cleared_When_Source_Format_Or_Merge_Changes()
    {
        _clipboard.Copy(_sheet.Range("A1:B2"));
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _clipboard.IsActive.Should().BeFalse();

        _clipboard.Copy(_sheet.Range("A1:B2"));
        _sheet.Cells.Merge(new Region(1, 2, 1, 1));
        _clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public void Entry_Is_Cleared_On_Row_Insert_And_Sheet_Removal()
    {
        _clipboard.Copy(_sheet.Range("A1:B2"));
        _sheet.Rows.InsertAt(10);
        _clipboard.IsActive.Should().BeFalse();

        var wb = new Workbook();
        var s1 = wb.AddSheet("One", 5, 5);
        wb.Clipboard.Copy(s1.Range(0, 0));
        wb.RemoveSheet("One");
        wb.Clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public void Paste_Copies_Block_At_Selection_Top_Left_And_Keeps_Entry()
    {
        Fill2X2();
        _clipboard.Copy(_sheet.Range("A1:B2"));
        var region = _clipboard.Paste(_sheet, new Region(4, 6, 4, 4));

        region.Should().BeEquivalentTo(new Region(4, 5, 4, 5));
        _sheet.Cells.GetValue(4, 4).Should().Be(1);
        _sheet.Cells.GetValue(5, 5).Should().Be(4);
        _sheet.Cells.GetValue(6, 4).Should().BeNull();
        _clipboard.IsActive.Should().BeTrue("a copied entry can be pasted again");
    }

    [Test]
    public void Paste_Tiles_Across_Exact_Multiple_Selection()
    {
        Fill2X2();
        _clipboard.Copy(_sheet.Range("A1:B2"));
        var region = _clipboard.Paste(_sheet, new Region(4, 7, 4, 9));

        region.Should().BeEquivalentTo(new Region(4, 7, 4, 9));
        for (var r = 4; r <= 7; r += 2)
        for (var c = 4; c <= 9; c += 2)
        {
            _sheet.Cells.GetValue(r, c).Should().Be(1);
            _sheet.Cells.GetValue(r + 1, c + 1).Should().Be(4);
        }

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(4, 4).Should().BeNull();
        _sheet.Cells.GetValue(7, 9).Should().BeNull();
    }

    [Test]
    public void Undo_Paste_Of_Formula_Into_Empty_Cell_Clears_Value()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.Cells.SetFormula(0, 1, "=$A$1*5");
        _clipboard.Copy(_sheet.Range("B1"));
        _clipboard.Paste(_sheet, new Region(4, 3));

        _sheet.Cells.HasFormula(4, 3).Should().BeTrue();
        _sheet.Cells.GetValue(4, 3).Should().Be(10.0);

        _sheet.Commands.Undo();
        _sheet.Cells.HasFormula(4, 3).Should().BeFalse();
        _sheet.Cells.GetValue(4, 3).Should().BeNull();
    }

    [Test]
    public void Undo_Paste_Of_Formula_Restores_Previous_Value()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.Cells.SetFormula(0, 1, "=$A$1*5");
        _sheet.Cells.SetValue(4, 3, "old");
        _clipboard.Copy(_sheet.Range("B1"));
        _clipboard.Paste(_sheet, new Region(4, 3));

        _sheet.Cells.GetValue(4, 3).Should().Be(10.0);

        _sheet.Commands.Undo();
        _sheet.Cells.HasFormula(4, 3).Should().BeFalse();
        _sheet.Cells.GetValue(4, 3).Should().Be("old");
    }

    [Test]
    public void Paste_Does_Not_Tile_Non_Multiple_Selection()
    {
        Fill2X2();
        _clipboard.Copy(_sheet.Range("A1:B2"));
        var region = _clipboard.Paste(_sheet, new Region(4, 6, 4, 6));
        region.Should().BeEquivalentTo(new Region(4, 5, 4, 5));
        _sheet.Cells.GetValue(6, 6).Should().BeNull();
    }

    [Test]
    public void Paste_Clips_To_Sheet_Edge()
    {
        Fill2X2();
        _clipboard.Copy(_sheet.Range("A1:B2"));
        var region = _clipboard.Paste(_sheet, new Region(19, 19));
        region.Should().BeEquivalentTo(new Region(19, 19, 19, 19));
        _sheet.Cells.GetValue(19, 19).Should().Be(1);
    }

    [Test]
    public void Cut_Paste_Moves_Clears_Entry_And_Never_Tiles()
    {
        Fill2X2();
        _sheet.Cells.SetFormula(0, 5, "=A1+B2");
        _clipboard.Cut(_sheet.Range("A1:B2"));
        var region = _clipboard.Paste(_sheet, new Region(4, 7, 4, 9));

        region.Should().BeEquivalentTo(new Region(4, 5, 4, 5));
        _sheet.Cells.GetValue(4, 4).Should().Be(1);
        _sheet.Cells.GetValue(6, 4).Should().BeNull();
        _sheet.Cells.GetValue(0, 0).Should().BeNull();
        _sheet.Cells.GetFormulaString(0, 5).Should().Be("=E5+F6");
        _clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public void Paste_Values_Only_Writes_Computed_Values_Without_Formulas_Or_Formats()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.Cells.SetFormula(1, 0, "=A1*3");
        _sheet.SetFormat(new Region(0, 1, 0, 0), new CellFormat { BackgroundColor = "red" });
        _clipboard.Copy(_sheet.Range("A1:A2"));
        _clipboard.Paste(_sheet, new Region(0, 4), PasteMode.Values);

        _sheet.Cells.GetValue(1, 4).Should().Be(6);
        _sheet.Cells.HasFormula(1, 4).Should().BeFalse();
        _sheet.GetFormat(0, 4).BackgroundColor.Should().BeNull();
    }

    [Test]
    public void Paste_Formats_Only_Leaves_Values_Alone()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.Cells.SetValue(0, 4, "keep");
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _clipboard.Copy(_sheet.Range("A1"));
        _clipboard.Paste(_sheet, new Region(0, 4), PasteMode.Formats);

        _sheet.Cells.GetValue(0, 4).Should().Be("keep");
        _sheet.GetFormat(0, 4).BackgroundColor.Should().Be("red");
    }

    [Test]
    public void BeforePaste_Can_Cancel_Or_Change_Mode()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });
        _clipboard.Copy(_sheet.Range("A1"));

        _clipboard.BeforePaste += (_, e) => e.Cancel = true;
        _clipboard.Paste(_sheet, new Region(0, 4)).Should().BeNull();
        _sheet.Cells.GetValue(0, 4).Should().BeNull();

        _clipboard.BeforePaste += (_, e) =>
        {
            e.Cancel = false;
            e.Mode = PasteMode.Values;
            e.IsInternal.Should().BeTrue();
        };
        _clipboard.Paste(_sheet, new Region(0, 4)).Should().NotBeNull();
        _sheet.Cells.GetValue(0, 4).Should().Be(2);
        _sheet.GetFormat(0, 4).BackgroundColor.Should().BeNull();
    }

    [Test]
    public void Pasted_Event_Reports_Region_And_Mode()
    {
        Fill2X2();
        _clipboard.Copy(_sheet.Range("A1:B2"));
        PastedEventArgs? args = null;
        _clipboard.Pasted += (_, e) => args = e;
        _clipboard.Paste(_sheet, new Region(4, 4));
        args!.Region.Should().BeEquivalentTo(new Region(4, 5, 4, 5));
        args.WasCut.Should().BeFalse();
        args.Mode.Should().Be(PasteMode.All);
        args.Sheet.Should().BeSameAs(_sheet);
    }

    [Test]
    public void Paste_Across_Sheets_In_Workbook()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("One", 10, 10);
        var s2 = wb.AddSheet("Two", 10, 10);
        s1.Cells.SetValue(0, 0, 5);
        s1.Cells.SetFormula(1, 0, "=A1");
        wb.Clipboard.Copy(s1.Range("A1:A2"));
        var region = wb.Clipboard.Paste(s2, new Region(2, 2));
        region.Should().BeEquivalentTo(new Region(2, 3, 2, 2));
        s2.Cells.GetValue(2, 2).Should().Be(5);
        s2.Cells.GetFormulaString(3, 2).Should().Be("=C3");
        s2.Cells.GetValue(3, 2).Should().Be(5);
    }

    [Test]
    public void Paste_Refuses_ReadOnly_Target_And_CanPaste_Agrees()
    {
        Fill2X2();
        _sheet.SetFormat(new Region(5, 5), new CellFormat { IsReadOnly = true });
        _clipboard.Copy(_sheet.Range("A1:B2"));
        _clipboard.CanPaste(_sheet, new Region(4, 4)).Should().BeFalse();
        _clipboard.Paste(_sheet, new Region(4, 4)).Should().BeNull();
        _clipboard.CanPaste(_sheet, new Region(8, 8)).Should().BeTrue();
        _clipboard.CanPaste(_sheet, new Region(4, 4), PasteMode.Formats).Should().BeTrue();
    }

    [Test]
    public void Nothing_To_Paste_Returns_Null()
    {
        _clipboard.CanPaste(_sheet, new Region(0, 0)).Should().BeFalse();
        _clipboard.Paste(_sheet, new Region(0, 0)).Should().BeNull();
    }

    [Test]
    public void TileOrigins_Match_Excel_Rules()
    {
        var source = new Region(0, 1, 0, 1);
        PasteTargetCalculator.GetTileOrigins(source, new Region(3, 3), true).Should().Equal(new CellPosition(3, 3));
        PasteTargetCalculator.GetTileOrigins(source, new Region(0, 3, 0, 1), true)
            .Should().Equal(new CellPosition(0, 0), new CellPosition(2, 0));
        PasteTargetCalculator.GetTileOrigins(source, new Region(0, 4, 0, 1), true).Should().Equal(new CellPosition(0, 0));
        PasteTargetCalculator.GetTileOrigins(source, new Region(0, 3, 0, 1), false).Should().Equal(new CellPosition(0, 0));
    }
}
