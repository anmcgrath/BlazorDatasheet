using System.Linq;
using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Clipboard;

public class WorkbookClipboardInsertTests
{
    private Sheet _sheet = null!;
    private WorkbookClipboard _clipboard = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(10, 10);
        _clipboard = _sheet.Workbook.Clipboard;
    }

    private void FillRow(params string[] values)
    {
        for (var i = 0; i < values.Length; i++)
            _sheet.Cells.SetValue(0, i, values[i]);
    }

    private void FillColumn(params string[] values)
    {
        for (var i = 0; i < values.Length; i++)
            _sheet.Cells.SetValue(i, 0, values[i]);
    }

    private object?[] Row(int count) => Enumerable.Range(0, count).Select(c => _sheet.Cells.GetValue(0, c)).ToArray();

    private object?[] Column(int count) =>
        Enumerable.Range(0, count).Select(r => _sheet.Cells.GetValue(r, 0)).ToArray();

    [Test]
    public void Cut_PasteInsert_Moves_Block_And_Shifts_Right()
    {
        FillRow("a", "b", "c", "d");
        _clipboard.Cut(_sheet.Range("B1"));
        PastedEventArgs? args = null;
        _clipboard.Pasted += (_, e) => args = e;

        var region = _clipboard.PasteInsert(_sheet, new Region(0, 3), Axis.Col);

        Row(4).Should().Equal("a", "c", "b", "d");
        region.Should().BeEquivalentTo(new Region(0, 2));
        _clipboard.IsActive.Should().BeFalse();
        args!.Region.Should().BeEquivalentTo(new Region(0, 2));
        args.WasCut.Should().BeTrue();
        args.Mode.Should().Be(PasteMode.All);
        args.Sheet.Should().BeSameAs(_sheet);
    }

    [Test]
    public void Cut_PasteInsert_Shifts_Down()
    {
        FillColumn("a", "b", "c", "d");
        _clipboard.Cut(_sheet.Range("A4"));

        var region = _clipboard.PasteInsert(_sheet, new Region(1, 0), Axis.Row);

        Column(4).Should().Equal("a", "d", "b", "c");
        region.Should().BeEquivalentTo(new Region(1, 0));
        _clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public void Copy_PasteInsert_Shifts_Right_And_Copies()
    {
        FillRow("a", "b", "c", "d");
        var entry = _clipboard.Copy(_sheet.Range("A1"));
        PastedEventArgs? args = null;
        _clipboard.Pasted += (_, e) => args = e;

        var region = _clipboard.PasteInsert(_sheet, new Region(0, 2), Axis.Col);

        Row(5).Should().Equal("a", "b", "a", "c", "d");
        region.Should().BeEquivalentTo(new Region(0, 2));
        args!.WasCut.Should().BeFalse();
        args.Region.Should().BeEquivalentTo(new Region(0, 2));
        _clipboard.Current.Should().BeSameAs(entry);
    }

    [Test]
    public void Copy_PasteInsert_Shifts_Down_And_Copies_Block()
    {
        _sheet.Cells.SetValue(0, 0, "x");
        _sheet.Cells.SetValue(0, 1, "y");
        _sheet.Cells.SetValue(1, 0, "p");
        _sheet.Cells.SetValue(1, 1, "q");
        _sheet.Cells.SetValue(5, 5, "keep");
        _clipboard.Copy(_sheet.Range("F6"));

        var region = _clipboard.PasteInsert(_sheet, new Region(1, 0), Axis.Row);

        region.Should().BeEquivalentTo(new Region(1, 0));
        _sheet.Cells.GetValue(0, 0).Should().Be("x");
        _sheet.Cells.GetValue(1, 0).Should().Be("keep");
        _sheet.Cells.GetValue(2, 0).Should().Be("p");
        // Only the column the block covers shifts.
        _sheet.Cells.GetValue(1, 1).Should().Be("q");
        _sheet.Cells.GetValue(5, 5).Should().Be("keep");
    }

    [Test]
    public void Copy_PasteInsert_Before_Source_Copies_Shifted_Source()
    {
        FillRow("a", "b", "c", "d");
        _clipboard.Copy(_sheet.Range("D1"));

        _clipboard.PasteInsert(_sheet, new Region(0, 1), Axis.Col).Should().BeEquivalentTo(new Region(0, 1));

        Row(5).Should().Equal("a", "d", "b", "c", "d");
        // The source has shifted away from the cells the entry refers to.
        _clipboard.IsActive.Should().BeFalse();
    }

    [Test]
    public void Copy_PasteInsert_Across_Sheets()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("One", 10, 10);
        var s2 = wb.AddSheet("Two", 10, 10);
        s1.Cells.SetValue(0, 0, "src");
        s2.Cells.SetValue(0, 0, "a");
        wb.Clipboard.Copy(s1.Range("A1"));

        wb.Clipboard.PasteInsert(s2, new Region(0, 0), Axis.Col).Should().BeEquivalentTo(new Region(0, 0));

        s2.Cells.GetValue(0, 0).Should().Be("src");
        s2.Cells.GetValue(0, 1).Should().Be("a");
        s1.Cells.GetValue(0, 0).Should().Be("src");
    }

    [Test]
    public void CanPasteInsert_False_With_Empty_Clipboard()
    {
        _clipboard.CanPasteInsert(_sheet, new Region(0, 0), Axis.Col).Should().BeFalse();
        _clipboard.PasteInsert(_sheet, new Region(0, 0), Axis.Col).Should().BeNull();
    }

    [Test]
    public void CanPasteInsert_False_For_ReadOnly_Target()
    {
        FillRow("a", "b", "c", "d");
        _sheet.SetFormat(new Region(0, 5), new CellFormat { IsReadOnly = true });

        _clipboard.Copy(_sheet.Range("A1"));
        _clipboard.CanPasteInsert(_sheet, new Region(0, 3), Axis.Col).Should().BeFalse();
        _clipboard.PasteInsert(_sheet, new Region(0, 3), Axis.Col).Should().BeNull();
        _clipboard.CanPasteInsert(_sheet, new Region(1, 3), Axis.Col).Should().BeTrue();

        _clipboard.Cut(_sheet.Range("B1"));
        _clipboard.CanPasteInsert(_sheet, new Region(0, 3), Axis.Col).Should().BeFalse();
        _clipboard.PasteInsert(_sheet, new Region(0, 3), Axis.Col).Should().BeNull();
        _clipboard.IsActive.Should().BeTrue();
        Row(4).Should().Equal("a", "b", "c", "d");
    }

    [Test]
    public void BeforePaste_Can_Cancel_Insert_Paste()
    {
        FillRow("a", "b", "c", "d");
        _clipboard.Cut(_sheet.Range("B1"));
        _clipboard.BeforePaste += (_, e) => e.Cancel = true;

        _clipboard.PasteInsert(_sheet, new Region(0, 3), Axis.Col).Should().BeNull();
        Row(4).Should().Equal("a", "b", "c", "d");
        _clipboard.IsActive.Should().BeTrue();
    }

    [Test]
    public void Insert_Paste_Refuses_Modes_Other_Than_All()
    {
        FillRow("a", "b", "c", "d");
        _clipboard.Copy(_sheet.Range("A1"));
        _clipboard.BeforePaste += (_, e) => e.Mode = PasteMode.Values;

        _clipboard.PasteInsert(_sheet, new Region(0, 2), Axis.Col).Should().BeNull();
        Row(4).Should().Equal("a", "b", "c", "d");
    }

    [Test]
    public void Undo_Cut_PasteInsert_Restores_In_One_Step()
    {
        FillRow("a", "b", "c", "d");
        _clipboard.Cut(_sheet.Range("B1"));
        _clipboard.PasteInsert(_sheet, new Region(0, 3), Axis.Col);

        _sheet.Commands.Undo();

        Row(4).Should().Equal("a", "b", "c", "d");
    }

    [Test]
    public void Undo_Copy_PasteInsert_Restores_In_One_Step()
    {
        FillRow("a", "b", "c", "d");
        _clipboard.Copy(_sheet.Range("A1"));
        _clipboard.PasteInsert(_sheet, new Region(0, 2), Axis.Col);

        _sheet.Commands.Undo();

        Row(5).Should().Equal("a", "b", "c", "d", null);
    }
}
