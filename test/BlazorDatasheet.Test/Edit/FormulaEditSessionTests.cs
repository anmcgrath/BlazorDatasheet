using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Edit;

public class FormulaEditSessionTests
{
    private Sheet _sheet = null!;
    private FormulaEditSession Session => _sheet.Editor.FormulaEdit;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(20, 20);
    }

    private void BeginEdit(string value, bool softEdit = false, int row = 0, int col = 0)
    {
        _sheet.Editor.BeginEdit(row, col, softEdit);
        _sheet.Editor.EditValue = value;
        Session.IsPickingEnabled = true;
    }

    private void Click(int row, int col, bool ctrl = false)
    {
        Session.HandlePointerDown(row, col, false, ctrl, false).Should().BeTrue();
        Session.HandlePointerUp().Should().BeTrue();
    }

    [Test]
    [TestCase("=", true)]
    [TestCase("=A1+", true)]
    [TestCase("=A1 + ", true)]
    [TestCase("=SUM(", true)]
    [TestCase("=SUM(A1,", true)]
    [TestCase("=A1:", true)]
    [TestCase("=A1>=", true)]
    [TestCase("=A1", false)]
    [TestCase("=SUM(A1)", false)]
    [TestCase("=1", false)]
    [TestCase("=SU", false)]
    [TestCase("hello", false)]
    [TestCase("", false)]
    public void Can_Accept_Reference_At_End_Of_Text(string text, bool expected)
    {
        BeginEdit(text);
        Session.CanAcceptReference.Should().Be(expected);
    }

    [Test]
    public void Can_Accept_Reference_Is_Evaluated_At_The_Caret()
    {
        BeginEdit("=SUM()+1");
        Session.CanAcceptReference.Should().BeFalse();
        Session.SetTextSelection(5, 5);
        Session.CanAcceptReference.Should().BeTrue();
    }

    [Test]
    public void Dragging_With_The_Pointer_Writes_The_Region_Into_The_Text()
    {
        BeginEdit("=SUM(");
        var focusRequested = false;
        Session.FocusRequested += (_, _) => focusRequested = true;

        Session.HandlePointerDown(1, 1, false, false, false).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=SUM(B2");
        Session.IsDragging.Should().BeTrue();

        Session.HandlePointerOver(2, 2).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=SUM(B2:C3");

        Session.HandlePointerUp().Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=SUM(B2:C3");
        Session.IsDragging.Should().BeFalse();
        Session.PendingCaret.Should().Be("=SUM(B2:C3".Length);
        focusRequested.Should().BeTrue();
    }

    [Test]
    public void Picking_Again_Replaces_The_Picked_Reference()
    {
        BeginEdit("=SUM(");
        Click(1, 1);
        Click(4, 4);
        _sheet.Editor.EditValue.Should().Be("=SUM(E5");
    }

    [Test]
    public void Picking_With_Ctrl_Adds_A_Reference()
    {
        BeginEdit("=SUM(");
        Click(1, 1);
        Click(4, 4, ctrl: true);
        _sheet.Editor.EditValue.Should().Be("=SUM(B2,E5");
    }

    [Test]
    public void Typing_Ends_The_Pick()
    {
        BeginEdit("=SUM(");
        Click(1, 1);
        _sheet.Editor.EditValue = "=SUM(B2,";
        Session.IsPicking.Should().BeFalse();
        Click(4, 4);
        _sheet.Editor.EditValue.Should().Be("=SUM(B2,E5");
    }

    [Test]
    public void Reference_Is_Inserted_At_The_Caret()
    {
        BeginEdit("=SUM()+1");
        Session.SetTextSelection(5, 5);
        Click(1, 1);
        _sheet.Editor.EditValue.Should().Be("=SUM(B2)+1");
        Session.PendingCaret.Should().Be(7);

        _sheet.Editor.EditValue = "=SUM(B2)+12";
        Session.PendingCaret.Should().BeNull("the text was changed by typing");
    }

    [Test]
    public void Selected_Text_Is_Replaced_By_The_Reference()
    {
        BeginEdit("=SUM(123)+1");
        Session.SetTextSelection(5, 8);
        Click(1, 1);
        _sheet.Editor.EditValue.Should().Be("=SUM(B2)+1");
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void Reference_At_The_Caret_Is_Replaced(int caret)
    {
        BeginEdit("=A1+B2");
        Session.SetTextSelection(caret, caret);
        Click(3, 3);
        _sheet.Editor.EditValue.Should().Be("=D4+B2");
    }

    [Test]
    public void Moving_The_Caret_Away_Ends_The_Pick()
    {
        BeginEdit("=A1+");
        Click(1, 1);
        Session.SetTextSelection(3, 3);
        Session.IsPicking.Should().BeFalse();
        Click(3, 3);
        _sheet.Editor.EditValue.Should().Be("=D4+B2");
    }

    [Test]
    public void Lost_Text_Selection_Is_Ignored()
    {
        BeginEdit("=SUM()+1");
        Session.SetTextSelection(5, 5);
        Session.SetTextSelection(-1, -1);
        Session.SelectionStart.Should().Be(5);
    }

    [Test]
    public void Pointer_Is_Not_Used_When_A_Reference_Cannot_Go_At_The_Caret()
    {
        BeginEdit("=SUM(A1)");
        Session.HandlePointerDown(1, 1, false, false, false).Should().BeFalse();
        Session.HandlePointerOver(2, 2).Should().BeFalse();
        Session.HandlePointerUp().Should().BeFalse();
        _sheet.Editor.EditValue.Should().Be("=SUM(A1)");
    }

    [Test]
    public void Pointer_Is_Not_Used_Unless_Picking_Is_Enabled_And_The_Text_Is_A_Formula()
    {
        BeginEdit("=SUM(");
        Session.IsPickingEnabled = false;
        Session.HandlePointerDown(1, 1, false, false, false).Should().BeFalse();

        _sheet.Editor.CancelEdit();
        BeginEdit("SUM(");
        Session.HandlePointerDown(1, 1, false, false, false).Should().BeFalse();
    }

    [Test]
    public void Arrow_Keys_Pick_From_The_Edit_Cell_During_A_Soft_Edit()
    {
        BeginEdit("=", softEdit: true, row: 2, col: 2);

        Session.HandleArrowKey(new Offset(1, 0), false).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=C4");

        Session.HandleArrowKey(new Offset(1, 0), false).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=C5");

        Session.HandleArrowKey(new Offset(1, 0), true).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=C5:C6");
    }

    [Test]
    public void Arrow_Keys_Do_Not_Pick_Outside_A_Soft_Edit_Or_After_A_Complete_Reference()
    {
        BeginEdit("=", softEdit: false);
        Session.HandleArrowKey(new Offset(1, 0), false).Should().BeFalse();
        _sheet.Editor.CancelEdit();

        BeginEdit("=A1", softEdit: true);
        Session.HandleArrowKey(new Offset(1, 0), false).Should().BeFalse();
        _sheet.Editor.EditValue.Should().Be("=A1");
    }

    [Test]
    public void Replace_Reference_Keeps_Sheet_Name_And_Fixed_Parts()
    {
        BeginEdit("=SUM('My Sheet'!$A$1:B2)+C3");
        Session.ReplaceReference(0, new Region(1, 2, 1, 2)).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=SUM('My Sheet'!$B$2:C3)+C3");

        Session.ReplaceReference(1, new Region(5, 5)).Should().BeTrue();
        _sheet.Editor.EditValue.Should().Be("=SUM('My Sheet'!$B$2:C3)+F6");

        Session.ReplaceReference(2, new Region(5, 5)).Should().BeFalse();
    }

    [Test]
    public void Replace_Text_Leaves_Caret_After_The_New_Text()
    {
        BeginEdit("=SU+1");
        Session.ReplaceText(1, 2, "SUM(");
        _sheet.Editor.EditValue.Should().Be("=SUM(+1");
        Session.PendingCaret.Should().Be(5);
        Session.SelectionStart.Should().Be(5);
    }

    [Test]
    public void References_Are_Scanned_As_The_Text_Changes()
    {
        var changes = 0;
        Session.ReferencesChanged += (_, _) => changes++;

        BeginEdit("=A1+B2:C3");
        Session.References.Select(x => x.Region).Should()
            .BeEquivalentTo(new IRegion[] { new Region(0, 0), new Region(1, 2, 1, 2) });
        changes.Should().BeGreaterThan(0);

        _sheet.Editor.EditValue = "A1";
        Session.References.Should().BeEmpty();
    }

    [Test]
    public void Named_References_Are_Resolved_And_Take_A_Color()
    {
        _sheet.NamedRanges.Set("myName", "B2:B3");
        BeginEdit("=myName+A1");

        Session.References.Should().HaveCount(2);
        Session.References[0].Kind.Should().Be(FormulaReferenceSpanKind.Named);
        Session.References[0].Region.Should().BeEquivalentTo(new Region(1, 2, 1, 1));
        Session.References.Select(x => x.ColorIndex).Should().Equal(1, 2);
    }

    [Test]
    public void Finishing_The_Edit_Resets_The_Session()
    {
        BeginEdit("=SUM(");
        Session.SetInputOwner(this);
        Session.HandlePointerDown(1, 1, false, false, false);

        var draggingChanges = 0;
        Session.DraggingChanged += (_, dragging) =>
        {
            dragging.Should().BeFalse();
            draggingChanges++;
        };

        _sheet.Editor.CancelEdit();

        draggingChanges.Should().Be(1);
        Session.References.Should().BeEmpty();
        Session.IsPicking.Should().BeFalse();
        Session.IsDragging.Should().BeFalse();
        Session.IsPickingEnabled.Should().BeFalse();
        Session.InputOwner.Should().BeNull();
    }
}
