using System.Linq;
using BlazorDatasheet.Model;
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
        Assert.AreEqual(1, sm.ActiveSelection.Range.Area);
        Assert.IsEmpty(sm.Selections);
        Assert.AreEqual(SelectionMode.Cell, sm.ActiveSelection.Mode);

        // Expand the selecting to row = 2, col = 2.
        // The total size should now be 4 and we 
        // should still be selecting
        sm.UpdateSelectingEndPosition(2, 2);
        Assert.NotNull(sm.ActiveSelection);
        Assert.AreEqual(4, sm.ActiveSelection.Range.Area);
        Assert.AreEqual(2, sm.ActiveSelection.Range.RowEnd);
        Assert.AreEqual(2, sm.ActiveSelection.Range.ColEnd);
    }

    [Test]
    public void Move_Selection_Single_Cell_Moves_Selection()
    {
        var sm = new SelectionManager(_sheet);
        sm.BeginSelectingCell(0, 0);
        sm.EndSelecting();

        sm.MoveSelection(1, 1);
        Assert.Null(sm.ActiveSelection);
        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(1, sm.Selections.First().Range.Area);
        Assert.AreEqual(1, sm.Selections.First().Range.RowStart);
        Assert.AreEqual(1, sm.Selections.First().Range.ColStart);
    }

    [Test]
    public void Set_Selection_Sets_Selection_Correctly()
    {
        var sm = new SelectionManager(_sheet);
        sm.SetSelection(1, 1);
        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(1, sm.Selections.First().Range.RowStart);
        Assert.AreEqual(1, sm.Selections.First().Range.ColStart);

        sm.SetSelection(2, 2);
        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(2, sm.Selections.First().Range.RowStart);
        Assert.AreEqual(2, sm.Selections.First().Range.ColStart);
        Assert.AreEqual(1, sm.Selections.First().Range.Area);
    }

    [Test]
    public void Update_Selection_After_Column_Selection_Updates_Correctly()
    {
        var sm = new SelectionManager(_sheet);
        sm.BeginSelectingCol(0);
        Assert.AreEqual(1, sm.ActiveSelection.Range.Width);
        Assert.AreEqual(3, sm.ActiveSelection.Range.Height);
        sm.UpdateSelectingEndPosition(0, 1);
        Assert.AreEqual(2, sm.ActiveSelection.Range.Width);
    }

    [Test]
    public void Update_Selection_After_Row_Selection_Selects_Correctly()
    {
        var sm = new SelectionManager(_sheet);
        sm.BeginSelectingRow(0);
        Assert.AreEqual(1, sm.ActiveSelection.Range.Height);
        Assert.AreEqual(3, sm.ActiveSelection.Range.Width);
        sm.UpdateSelectingEndPosition(1, 0);
        Assert.AreEqual(2, sm.ActiveSelection.Range.Height);
        Assert.AreEqual(3, sm.ActiveSelection.Range.Width);
    }

    [Test]
    public void Extend_Selection_Correctly_Extends_Selection()
    {
        var sm = new SelectionManager(_sheet);
        sm.SetSelection(0, 1);
        sm.ExtendSelection(2, 2);
        sm.EndSelecting();
        Assert.AreEqual(3, sm.Selections.First().Range.Height);
        Assert.AreEqual(2, sm.Selections.First().Range.Width);
        Assert.AreEqual(1, sm.Selections.Count);
    }
}