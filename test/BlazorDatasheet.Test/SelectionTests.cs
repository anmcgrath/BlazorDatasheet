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
        _sheet = new Sheet(3, 3);
    }

    [Test]
    public void Add_Selection_As_Entire_Sheet_Selects_All_cells_And_Sets_Active()
    {
        var selection = new Selection(_sheet);
        //Select all
        selection.SetSingle(_sheet.Range);
        var cells = selection.GetCells().ToList();
        Assert.AreEqual(_sheet.Range.Area, cells.Count);
        Assert.AreEqual(_sheet.GetCell(0, 0), cells[0]);
    }

    public void Add_Selection_Changes_Active_Selection()
    {
        var selection = new Selection(_sheet);
        var r = new Range(0, 0);
        selection.Add(r);
        Assert.AreEqual(r, selection.ActiveRange);
        Assert.AreEqual(r.Start, selection.ActiveCellPosition);
        var r2 = new Range(1, 1);
        Assert.AreEqual(r2, selection.ActiveRange);
        Assert.AreEqual(r2.Start, selection.ActiveCellPosition);
    }

    public void Add_Selection_Outside_Range_Constrains_Inside()
    {
        var selection = new Selection(_sheet);
        selection.Add(new Range(-1000, 1000, -1000, 1000));
        Assert.AreEqual(_sheet.Range.Area, selection.ActiveRange.GetIntersection(_sheet.Range).Area);
    }

    [Test]
    public void Cycle_Active_position_through_range()
    {
        var selection = new Selection(_sheet);
        selection.Add(new Range(0,1,0,1));
        Assert.AreEqual(new CellPosition(0,0), selection.ActiveCellPosition);
        selection.MoveActivePosition(1);
        Assert.AreEqual(new CellPosition(1,0), selection.ActiveCellPosition);
        selection.MoveActivePosition(1);
        Assert.AreEqual(new CellPosition(0,1), selection.ActiveCellPosition);
        selection.MoveActivePosition(1);
        Assert.AreEqual(new CellPosition(1,1), selection.ActiveCellPosition);
        selection.MoveActivePosition(1);
        Assert.AreEqual(new CellPosition(0,0), selection.ActiveCellPosition);
    }

    [Test]
    public void Cycle_Active_position_through_ranges()
    {
        var selection = new Selection(_sheet);
        var r1 = new Range(0, 0, 0, 0);
        var r2 = new Range(1, 1, 1, 1);
        
        selection.Add(r1);
        selection.Add(r2);
        
        Assert.AreEqual(r2, selection.ActiveRange);
        Assert.AreEqual(r2.Start, selection.ActiveCellPosition);
        
        selection.MoveActivePosition(1);
        
        Assert.AreEqual(r1, selection.ActiveRange);
        Assert.AreEqual(r1.Start, selection.ActiveCellPosition);
    }

    [Test]
    public void Selection_Event_Changed_Fires_Correctly()
    {
        var nTimesChanged = 0;
        var selection = new Selection(_sheet);
        selection.Changed += (sender, ranges) =>
        {
            nTimesChanged++;
        };
        
        selection.SetSingle(new Range(1,1));
        selection.Add(new Range(2,2));
        selection.Clear();
        Assert.AreEqual(3, nTimesChanged);
    }
}