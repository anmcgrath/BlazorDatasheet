using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

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
        selection.Set(_sheet.Region);
        var posns = selection.Ranges.SelectMany(x => x.Positions).ToList();
        var cells = posns.Select(x => _sheet.Cells.GetCell(x.row, x.col)).ToList();
        Assert.AreEqual(_sheet.Region.Area, cells.Count);
    }

    [Test]
    public void Add_Selection_Outside_Region_Constrains_Inside()
    {
        var selection = new Selection(_sheet);
        selection.Set(new Region(-1000, 1000, -1000, 1000));
        Assert.AreEqual(_sheet.Region.Area, selection.ActiveRegion.GetIntersection(_sheet.Region).Area);
    }

    [Test]
    public void Cycle_Active_position_through_range()
    {
        var selection = new Selection(_sheet);
        selection.Set(new Region(0, 1, 0, 1));


        Assert.AreEqual(new CellPosition(0, 0), selection.ActiveCellPosition);
        selection.MoveActivePositionByRow(1);
        Assert.AreEqual(new CellPosition(1, 0), selection.ActiveCellPosition);
        selection.MoveActivePositionByRow(1);
        Assert.AreEqual(new CellPosition(0, 1), selection.ActiveCellPosition);
        selection.MoveActivePositionByRow(1);
        Assert.AreEqual(new CellPosition(1, 1), selection.ActiveCellPosition);
        selection.MoveActivePositionByRow(1);
        Assert.AreEqual(new CellPosition(0, 0), selection.ActiveCellPosition);
    }

    [Test]
    public void Cycle_Active_position_through_ranges()
    {
        var selection = new Selection(_sheet);
        var r1 = new Region(0, 0, 0, 0);
        var r2 = new Region(1, 1, 1, 1);

        selection.Set(0, 0);
        selection.BeginSelectingCell(1, 1);
        selection.EndSelecting();

        Assert.AreEqual(r2, selection.ActiveRegion);
        Assert.AreEqual(r2.TopLeft, selection.ActiveCellPosition);

        selection.MoveActivePositionByRow(1);

        Assert.AreEqual(r1, selection.ActiveRegion);
        Assert.AreEqual(r1.TopLeft, selection.ActiveCellPosition);
    }

    [Test]
    public void Selection_Event_Changed_Fires_Correctly()
    {
        var nTimesChanged = 0;
        var selection = new Selection(_sheet);
        selection.SelectionChanged += (sender, ranges) => { nTimesChanged++; };

        selection.Set(new Region(1, 1));
        selection.Set(new Region(2, 2));
        selection.ClearSelections();
        Assert.AreEqual(3, nTimesChanged);
    }

    [Test]
    public void Moves_Input_Position_When_Selecting_Region_Is_One_Cell()
    {
        _sheet.Selection.Set(0, 0);
        _sheet.Selection.MoveActivePositionByRow(1);
        Assert.AreEqual(new CellPosition(1, 0), _sheet.Selection.GetInputPosition());
        Assert.AreEqual(new CellPosition(1, 0), _sheet.Selection.ActiveCellPosition);
    }

    [Test]
    public void Active_Selection_Moves_Over_Merged_cells()
    {
        _sheet.Cells.Merge(new Region(0, 1, 0, 1));
        _sheet.Selection.Set(0, 0);
        Assert.AreEqual(4, _sheet.Selection.ActiveRegion.Area);
        _sheet.Selection.MoveActivePositionByRow(1);
        Assert.AreEqual(new CellPosition(2, 0), _sheet.Selection.ActiveCellPosition);
    }

    [Test]
    public void Setting_Selection_Fully_Inside_Merged_Region_Expands_To_Merged_Region()
    {
        var merge = new Region(2, 3, 5, 6);
        _sheet.Cells.Merge(merge);
        _sheet.Selection.Set(new Region(2, 2, 5, 5));
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(merge);
    }

    [Test]
    public void Extend_Selection_To_Row_Col_Extends_Correctly()
    {
        _sheet.Selection.Set(1, 1);
        _sheet.Selection.ExtendTo(2, 2);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 2, 1, 2));
    }

    [Test]
    public void Active_Region_Is_Same_As_Last_Selected_Region_Added()
    {
        var r2 = new Region(1, 1, 1, 1);
        _sheet.Selection.Set([new Region(0, 0, 0, 0), r2]);
        _sheet.Selection.ActiveRegion.Should().BeSameAs(_sheet.Selection.Regions.Last());
    }

    [Test]
    public void Contract_Region_Over_Merged_Regions_With_Single_Intersection_Works()
    {
        var merge = new Region(2, 3, 4, 5);
        _sheet.Cells.Merge(merge);
        // intersect on right edge of region
        var r = new Region(1, 5, 2, 4);
        _sheet.ContractRegionOverMerges(r)
            .Should()
            .BeEquivalentTo(new Region(1, 5, 2, 3));
    }

    [Test]
    public void Contract_Region_Over_Merged_Regions_When_Region_Fully_Inside_Merge_Sets_To_Null()
    {
        var merge = new Region(1, 4, 1, 4);
        _sheet.Cells.Merge(merge);
        _sheet.ContractRegionOverMerges(new Region(2, 3, 2, 3))
            .Should()
            .BeNull();
    }

    [Test]
    public void Contract_Region_Over_Merged_Regions_When_Region_Equal_Returns_Region()
    {
        var merge = new Region(1, 4, 1, 4);
        _sheet.Cells.Merge(merge);
        _sheet.ContractRegionOverMerges(merge)
            .Should()
            .BeEquivalentTo(merge);
    }

    [Test]
    public void Contract_Selection_Over_Merged_Regions_When_Merge_Fully_Inside_Region_Returns_Region()
    {
        _sheet.Cells.Merge(new Region(2, 3, 2, 3));
        var region = new Region(0, 10, 1, 11);
        _sheet.ContractRegionOverMerges(region)
            .Should()
            .BeEquivalentTo(region);
    }

    [Test]
    public void Contract_Selection_Over_Single_Cell_Should_Still_Contain_Cell_And_Should_Expand_Other_Edge()
    {
        var edges = new List<Edge> { Edge.Right, Edge.Left, Edge.Bottom, Edge.Top };
        var position = new Region(5, 5);
        foreach (var edge in edges)
        {
            _sheet.Selection.Set(position);
            _sheet.Selection.ContractEdge(edge, 1);
            _sheet.Selection.ActiveRegion!.Contains(position)
                .Should().BeTrue();
            // the opposite edge should have moved
            _sheet.Selection.ActiveRegion.GetEdgePosition(edge.GetOpposite())
                .Should()
                .NotBe(position.GetEdgePosition(edge.GetOpposite()));
        }
    }
}