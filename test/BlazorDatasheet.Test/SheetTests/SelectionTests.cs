using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render;
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
    public void Cycle_Active_Position_Backward_Through_Ranges()
    {
        var selection = new Selection(_sheet);
        var r1 = new Region(0, 0, 0, 0);
        var r2 = new Region(1, 1, 1, 1);

        selection.Set(new List<IRegion>() { r1, r2 });
        selection.MoveActivePositionByRow(-1);

        selection.ActiveRegion.Should().BeEquivalentTo(r1);
        selection.ActiveCellPosition.Should().Be(r1.TopLeft);
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
    public void End_Selecting_Fires_Selection_Changed_Once()
    {
        var selection = new Selection(_sheet);
        var nTimesChanged = 0;
        selection.SelectionChanged += (sender, ranges) => { nTimesChanged++; };

        selection.BeginSelectingCell(1, 1);
        selection.EndSelecting();

        nTimesChanged.Should().Be(1);
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
    public void Active_Selection_By_Col_Skips_Hidden_Columns()
    {
        _sheet.Columns.Hide(1, 1);
        _sheet.Selection.Set(new Region(0, 1, 0, 2));
        _sheet.Selection.MoveActivePositionByCol(1);
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 2));
    }

    [Test]
    public void Active_Selection_By_Col_Leaves_Merged_Cell_From_Edge()
    {
        _sheet.Cells.Merge(new Region(0, 1, 0, 1));
        _sheet.Selection.Set(new Region(0, 1, 0, 2));
        _sheet.Selection.MoveActivePositionByCol(1);
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(0, 2));
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

    [Test]
    public void Contract_Selection_Fallback_Fires_Selection_Changed_Once()
    {
        _sheet.Selection.Set(1, 1);
        var nTimesChanged = 0;
        _sheet.Selection.SelectionChanged += (sender, args) => { nTimesChanged++; };

        _sheet.Selection.ContractEdge(Edge.Bottom, 1);

        nTimesChanged.Should().Be(1);
    }

    [Test]
    public void Select_Row_Header_Then_Shift_Select_Row_Header_Should_Extend_Row_Selection()
    {
        var manager = new SelectionInputManager(_sheet.Selection);
        // Start selecting Row 2
        manager.HandlePointerDown(1, -1, false, false, false, 0);
        manager.HandleWindowMouseUp();
        // Extend selection to Row 3, should now be selecting rows 2:3
        manager.HandlePointerDown(2, -1, shift: true, false, false, 0);
        manager.HandleWindowMouseUp();
        manager.Selection.ActiveRegion.Should().BeOfType<RowRegion>();
        manager.Selection.ActiveRegion.Height.Should().Be(2);
        manager.Selection.ActiveRegion.Top.Should().Be(1);
    }

    [Test]
    public void Select_Col_Header_Then_Shift_Select_Col_Header_Should_Extend_Col_Selection()
    {
        var manager = new SelectionInputManager(_sheet.Selection);
        // Start selecting Col 
        manager.HandlePointerDown(-1, 1, false, false, false, 0);
        manager.HandleWindowMouseUp();
        // Extend selection to Row C, should now be selecting rows B:C
        manager.HandlePointerDown(-1, 2, shift: true, false, false, 0);
        manager.HandleWindowMouseUp();
        manager.Selection.ActiveRegion.Should().BeOfType<ColumnRegion>();
        manager.Selection.ActiveRegion.Width.Should().Be(2);
        manager.Selection.ActiveRegion.Left.Should().Be(1);
    }

    [Test]
    public void Active_Cell_Should_Move_To_Top_With_Row_Selection_And_Enter()
    {
        var manager = new SelectionInputManager(_sheet.Selection);
        manager.HandlePointerDown(-1, 1, false, false, false, 0);
        manager.HandleWindowMouseUp();

        for (int i = 0; i < _sheet.NumRows; i++)
        {
            manager.Selection.MoveActivePositionByRow(1);
        }

        manager.Selection.ActiveCellPosition.row.Should().Be(0);
    }

    [Test]
    public void Active_Cell_Should_Move_To_Left_With_Col_Selection_And_Enter()
    {
        var manager = new SelectionInputManager(_sheet.Selection);
        manager.HandlePointerDown(1, -1, false, false, false, 0);
        manager.HandleWindowMouseUp();

        for (int i = 0; i < _sheet.NumCols; i++)
        {
            manager.Selection.MoveActivePositionByCol(1);
        }

        manager.Selection.ActiveCellPosition.col.Should().Be(0);
    }

    [Test]
    public void Setting_Active_Region_In_Various_Ways_Fires_Active_Region_Event()
    {
        IRegion? oldRegion = null;
        IRegion? newRegion = null;

        _sheet.Selection.ActiveRegionChanged += (sender, ev) =>
        {
            oldRegion = ev.OldRegion;
            newRegion = ev.NewRegion;
        };

        var initialRegion = new Region(2, 2);


        var fns = new List<Action>()
        {
            () => _sheet.Selection.ExpandEdge(Edge.Bottom, 1),
            () => _sheet.Selection.ContractEdge(Edge.Bottom, 1),
            () =>
            {
                _sheet.Selection.BeginSelectingCell(10, 10);
                _sheet.Selection.EndSelecting();
            }
        };

        foreach (var fn in fns)
        {
            _sheet.Selection.Set(2, 2);
            fn.Invoke();
            newRegion.Should().BeEquivalentTo(_sheet.Selection.ActiveRegion);
            oldRegion.Should().BeEquivalentTo(initialRegion);
        }
    }

    [Test]
    public void HandleArrowKeyDown_OnSingleCellSelection_RaisesActiveRegionChanged_With_MovedRegion()
    {
        var manager = new SelectionInputManager(_sheet.Selection);
        manager.Selection.Set(0, 0);

        IRegion? newRegion = null;
        manager.Selection.ActiveRegionChanged += (sender, ev) => { newRegion = ev.NewRegion; };

        manager.HandleArrowKeyDown(false, new Offset(1, 0));

        newRegion.Should().BeEquivalentTo(new Region(1, 0));
        manager.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(1, 0));
    }

    [Test]
    public void Active_Position_Inside_Active_Region_Should_Set_To_PositionSpecified()
    {
        _sheet.Selection.Set(new List<IRegion>() { new Region(4, 5, 4, 5), new Region(0, 2, 0, 1) });
        _sheet.Selection.Activate(1, 1);
        _sheet.Selection.ActiveRegion.Left.Should().Be(0); //
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(1, 1));
    }

    [Test]
    public void Active_Position_Inside_Non_Active_Region_Should_Set_To_Active_Region()
    {
        _sheet.Selection.Set(new List<IRegion>() { new Region(4, 5, 4, 5), new Region(0, 2, 0, 1) });
        _sheet.Selection.Activate(4, 5);
        _sheet.Selection.ActiveRegion.Left.Should().Be(4); //
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(4, 5));
    }

    [Test]
    public void Active_Position_Inside_No_Regions_Should_Set_Active_region_and_position_appropriately()
    {
        _sheet.Selection.Set(new List<IRegion>() { new Region(4, 5, 4, 5), new Region(0, 2, 0, 1) });
        _sheet.Selection.Activate(9, 9);
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(9, 9));
        _sheet.Selection.ActiveCellPosition.Should().Be(new CellPosition(9, 9));
        _sheet.Selection.Regions.Count.Should().Be(1);
    }
}
