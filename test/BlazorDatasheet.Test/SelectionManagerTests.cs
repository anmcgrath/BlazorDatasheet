using System.Linq;
using BlazorDatasheet.Data;
using BlazorDatasheet.Selecting;
using NUnit.Framework;

namespace BlazorDatasheet.Test;

public class SelectionManagerTests
{
    private Sheet _sheet;

    [SetUp]
    public void Setup()
    {
        var cells = new Cell[3, 3]
        {
            { new Cell(""), new Cell(""), new Cell("") },
            { new Cell(""), new Cell(""), new Cell("") },
            { new Cell(""), new Cell(""), new Cell("") },
        };
        _sheet = new Sheet(3, 3, cells);
    }

    [Test]
    public void Standard_Selecting_Use_Case_Works_Correctly()
    {
        var sm = new SelectionManager(_sheet);

        // Begin selecting at row = 1, col = 1
        sm.BeginSelectingCell(1, 1);

        Assert.NotNull(sm.ActiveSelection);

        var activeSelectionRange = (IFixedSizeRange)sm.ActiveSelection!.Range;

        Assert.AreEqual(1, activeSelectionRange.Area);
        Assert.IsEmpty(sm.Selections);
        Assert.AreEqual(SelectionMode.Cell, sm.ActiveSelection.Mode);

        // Expand the selecting to row = 2, col = 2.
        // The total size should now be 4 and we 
        // should still be selecting
        sm.UpdateSelectingEndPosition(2, 2);
        Assert.NotNull(sm.ActiveSelection);

        activeSelectionRange = (IFixedSizeRange)sm.ActiveSelection!.Range;

        Assert.AreEqual(4, activeSelectionRange.Area);
        Assert.AreEqual(2, activeSelectionRange.EndPosition.Row);
        Assert.AreEqual(2, activeSelectionRange.EndPosition.Col);
    }

    [Test]
    public void Move_Selection_Single_Cell_Moves_Selection()
    {
        var sm = new SelectionManager(_sheet);
        sm.BeginSelectingCell(0, 0);
        sm.EndSelecting();

        sm.MoveSelection(1, 1);
        Assert.Null(sm.ActiveSelection);

        var firstSelection = (IFixedSizeRange)(sm.Selections.First().Range);

        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(1, firstSelection.Area);
        Assert.AreEqual(1, firstSelection.StartPosition.Row);
        Assert.AreEqual(1, firstSelection.StartPosition.Col);
    }

    [Test]
    public void Set_Selection_Sets_Selection_Correctly()
    {
        var sm = new SelectionManager(_sheet);
        sm.SetSelection(1, 1);
        Assert.AreEqual(1, sm.Selections.Count);

        var firstSelection = (IFixedSizeRange)(sm.Selections.First().Range);
        Assert.AreEqual(1, firstSelection.StartPosition.Row);
        Assert.AreEqual(1, firstSelection.StartPosition.Col);

        sm.SetSelection(2, 2);
        firstSelection = (IFixedSizeRange)(sm.Selections.First().Range);
        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(2, firstSelection.StartPosition.Row);
        Assert.AreEqual(2, firstSelection.StartPosition.Col);
        Assert.AreEqual(1, firstSelection.Area);
    }

    [Test]
    public void Update_Selection_After_Column_Selection_Updates_Correctly()
    {
        var sm = new SelectionManager(_sheet);
        sm.BeginSelectingCol(0);

        var activeRange = sm.ActiveSelection!.Range;
        var constrainedCol = activeRange.GetIntersection(_sheet.Range);


        Assert.AreEqual(1, constrainedCol.Width);
        Assert.AreEqual(3, constrainedCol.Height);
        sm.UpdateSelectingEndPosition(0, 1);
        constrainedCol = activeRange.GetIntersection(_sheet.Range);
        Assert.AreEqual(2, constrainedCol.Width);
    }

    [Test]
    public void Update_Selection_After_Row_Selection_Selects_Correctly()
    {
        var sm = new SelectionManager(_sheet);
        sm.BeginSelectingRow(0);
        var constrainedRow = sm.ActiveSelection!.Range.GetIntersection(_sheet.Range);
        Assert.AreEqual(1, constrainedRow.Height);
        Assert.AreEqual(3, constrainedRow.Width);
        sm.UpdateSelectingEndPosition(1, 0);
        constrainedRow = sm.ActiveSelection!.Range.GetIntersection(_sheet.Range);
        Assert.AreEqual(2, constrainedRow.Height);
        Assert.AreEqual(3, constrainedRow.Width);
    }

    [Test]
    public void Extend_Selection_Correctly_Extends_Selection()
    {
        var sm = new SelectionManager(_sheet);
        sm.SetSelection(0, 1);
        sm.ExtendSelection(2, 2);
        sm.EndSelecting();
        var firstSelectionRange = (IFixedSizeRange)sm.Selections.First().Range;
        Assert.AreEqual(3, firstSelectionRange.Height);
        Assert.AreEqual(2, firstSelectionRange.Width);
        Assert.AreEqual(1, sm.Selections.Count);
    }

    [Test]
    public void Move_Selection_Outside_Of_Sheet_Constrains_To_Sheet()
    {
        var sm = new SelectionManager(_sheet);
        sm.SetSelection(0, 0);
        sm.MoveSelection(-1, -1);
        var selectionRange = (IFixedSizeRange)sm.Selections.First().Range;
        Assert.AreEqual(0, selectionRange.StartPosition.Row);
        Assert.AreEqual(0, selectionRange.StartPosition.Col);
    }
}