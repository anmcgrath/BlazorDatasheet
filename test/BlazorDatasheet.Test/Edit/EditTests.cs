using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Edit;

public class EditTests
{
    [Test]
    public void Begin_Edit_Starts_Editing()
    {
        var sheet = new Sheet(10, 10);
        sheet.Editor.BeginEdit(1, 2);
        Assert.AreEqual(1, sheet.Editor.EditCell.Row);
        Assert.AreEqual(2, sheet.Editor.EditCell.Col);
    }

    [Test]
    public void Begin_Edit_Sets_Edit_Value()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(1, 1, "Test");

        sheet.Editor.BeginEdit(0, 0);
        Assert.True(string.IsNullOrEmpty(sheet.Editor.EditValue));
        sheet.Editor.CancelEdit();

        sheet.Editor.BeginEdit(1, 1);
        Assert.AreEqual("Test", sheet.Editor.EditValue);
    }

    [Test]
    public void Cannot_Begin_Editing_When_Currently_Editing()
    {
        var sheet = new Sheet(10, 10);
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.BeginEdit(1, 1);
        // The editor should not have started editing (1, 1) because we didn't finish (10, 10)
        Assert.AreEqual(0, sheet.Editor.EditCell.Row);
        Assert.AreEqual(0, sheet.Editor.EditCell.Col);
    }

    [Test]
    public void Cancel_Edit_Cancels_Edit()
    {
        var sheet = new Sheet(10, 10);
        sheet.Editor.BeginEdit(1, 1);
        sheet.Editor.CancelEdit();
        Assert.AreEqual(false, sheet.Editor.IsEditing);
        Assert.Null(sheet.Editor.EditCell);
        Assert.True(string.IsNullOrEmpty(sheet.Editor.EditValue));
    }

    [Test]
    public void Accept_Edit_Sets_Value()
    {
        var sheet = new Sheet(10, 10);
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "Test";
        sheet.Editor.AcceptEdit();
        Assert.AreEqual("Test", sheet.Cells.GetValue(0, 0));
    }

    [Test]
    public void Accept_Edit_Is_Undoable()
    {
        var sheet = new Sheet(10, 10);
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "Test";
        sheet.Editor.AcceptEdit();
        sheet.Commands.Undo();
        Assert.AreEqual(null, sheet.Cells.GetValue(0, 0));
    }

    [Test]
    public void Do_Not_Do_Conversion_If_Cell_Type_Is_Set_To_Text()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells[0, 0].Type = "text";
        sheet.Editor.BeginEdit(0, 0);
        sheet.Editor.EditValue = "04-10-1";
        sheet.Editor.AcceptEdit();
        sheet.Cells[0, 0].Value.Should().Be("04-10-1");
    }

    [Test]
    public void Do_Not_Start_Edit_If_Cell_Is_Readonly()
    {
        var sheet = new Sheet(10, 10);
        sheet.SetFormat(new Region(1, 1), new CellFormat() { IsReadOnly = true });
        sheet.Editor.BeginEdit(1, 1);
        sheet.Editor.IsEditing.Should().Be(false);
    }

    [Test]
    public void Cell_That_Is_not_visible_should_not_be_edited()
    {
        var sheet = new Sheet(10, 10);
        sheet.Rows.Hide(1, 1);
        sheet.Editor.BeginEdit(1, 1);
        sheet.Editor.IsEditing.Should().Be(false);
    }

    [Test]
    public void Empty_Edit_Value_Should_Clear_Cell()
    {
        var sheet = new Sheet(10, 10);
        sheet.Editor.BeginEdit(1, 1);
        sheet.Editor.EditValue = string.Empty;
        sheet.Editor.AcceptEdit();
        sheet.Cells.GetCell(1, 1).CellValue.ValueType.Should().Be(CellValueType.Empty);
    }
}