using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Edit;

public class ReferenceDragCalculatorTests
{
    private static readonly IRegion Bounds = new Region(0, 9, 0, 9);

    // regions are compared by their bounds
    private static void AssertRegion(IRegion actual, IRegion expected) =>
        actual.Equals(expected).Should().BeTrue($"{actual} should be {expected}");

    [Test]
    public void Move_Keeps_The_Size_And_The_Grabbed_Cell_Under_The_Pointer()
    {
        var moved = ReferenceDragCalculator.Move(new Region(1, 3, 1, 2), new CellPosition(3, 2),
            new CellPosition(5, 6), Bounds);
        AssertRegion(moved, new Region(3, 5, 5, 6));
    }

    [Test]
    [TestCase(-1, -1, 0, 0)]
    [TestCase(0, 0, 0, 0)]
    [TestCase(20, 20, 7, 8)]
    [TestCase(2, 20, 2, 8)]
    [TestCase(20, 2, 7, 2)]
    public void Move_Stays_Inside_The_Bounds(int row, int col, int expectedTop, int expectedLeft)
    {
        var moved = ReferenceDragCalculator.Move(new Region(2, 4, 2, 3), new CellPosition(2, 2),
            new CellPosition(row, col), Bounds);
        AssertRegion(moved, new Region(expectedTop, expectedTop + 2, expectedLeft, expectedLeft + 1));
    }

    [Test]
    [TestCase(1, 1, 6, 6, 1, 6, 1, 6)]
    [TestCase(3, 3, 0, 0, 0, 3, 0, 3)]
    [TestCase(1, 3, 5, 0, 1, 5, 0, 3)]
    [TestCase(3, 1, 0, 5, 0, 3, 1, 5)]
    public void Resize_Stretches_From_The_Anchor_To_The_Pointer(int anchorRow, int anchorCol, int row, int col,
        int top, int bottom, int left, int right)
    {
        var resized = ReferenceDragCalculator.Resize(new Region(1, 3, 1, 3), new CellPosition(anchorRow, anchorCol),
            new CellPosition(row, col), Bounds);
        AssertRegion(resized, new Region(top, bottom, left, right));
    }

    [Test]
    public void Resize_Flips_Past_The_Anchor_And_Collapses_Onto_It()
    {
        var original = new Region(2, 4, 2, 4);
        var anchor = new CellPosition(2, 2);

        AssertRegion(ReferenceDragCalculator.Resize(original, anchor, new CellPosition(0, 1), Bounds), new Region(0, 2, 1, 2));
        AssertRegion(ReferenceDragCalculator.Resize(original, anchor, new CellPosition(2, 2), Bounds), new Region(2, 2));
        AssertRegion(ReferenceDragCalculator.Resize(original, anchor, new CellPosition(-1, 50), Bounds), new Region(0, 2, 2, 9));
    }

    [Test]
    public void Column_Regions_Move_And_Resize_Across_Columns_Only()
    {
        var original = new ColumnRegion(1, 2);

        var moved = ReferenceDragCalculator.Move(original, new CellPosition(0, 1), new CellPosition(5, 4), Bounds);
        moved.Should().BeOfType<ColumnRegion>();
        (moved.Left, moved.Right).Should().Be((4, 5));

        ReferenceDragCalculator.Move(original, new CellPosition(0, 1), new CellPosition(5, 20), Bounds)
            .Left.Should().Be(8);

        var resized = ReferenceDragCalculator.Resize(original, new CellPosition(9, 1), new CellPosition(3, 6),
            Bounds);
        resized.Should().BeOfType<ColumnRegion>();
        (resized.Left, resized.Right).Should().Be((1, 6));
    }

    [Test]
    public void Row_Regions_Move_And_Resize_Down_Rows_Only()
    {
        var original = new RowRegion(1, 2);

        var moved = ReferenceDragCalculator.Move(original, new CellPosition(2, 0), new CellPosition(5, 4), Bounds);
        moved.Should().BeOfType<RowRegion>();
        (moved.Top, moved.Bottom).Should().Be((4, 5));

        var resized = ReferenceDragCalculator.Resize(original, new CellPosition(2, 9), new CellPosition(0, 6),
            Bounds);
        resized.Should().BeOfType<RowRegion>();
        (resized.Top, resized.Bottom).Should().Be((0, 2));
    }
}
