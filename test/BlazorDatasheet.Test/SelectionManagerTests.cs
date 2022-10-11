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
        sm.SetSelection(1,1);
        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(1, sm.Selections.First().Range.RowStart);
        Assert.AreEqual(1, sm.Selections.First().Range.ColStart);
        
        sm.SetSelection(2, 2);
        Assert.AreEqual(1, sm.Selections.Count);
        Assert.AreEqual(2, sm.Selections.First().Range.RowStart);
        Assert.AreEqual(2, sm.Selections.First().Range.ColStart);
        Assert.AreEqual(1, sm.Selections.First().Range.Area);
    }
}