using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render.Layers;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class AutofillDragCalculatorTests
{
    private const double ColWidth = 100;
    private const double RowHeight = 20;

    private Sheet _sheet = null!;
    private CellLayoutProvider _layoutProvider = null!;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(10, 10, ColWidth, RowHeight);
        _layoutProvider = new CellLayoutProvider(_sheet);
    }

    /// <summary>
    /// The centre of a cell, in sheet coordinates - the same space the layer feeds the calculator.
    /// </summary>
    private static (double x, double y) Centre(int row, int col)
        => (col * ColWidth + ColWidth / 2, row * RowHeight + RowHeight / 2);

    /// <summary>
    /// The fill handle sits on the bottom right corner of the selection, in sheet coordinates.
    /// </summary>
    private static (double x, double y) FillHandle(IRegion region)
        => ((region.Right + 1) * ColWidth, (region.Bottom + 1) * RowHeight);

    private IRegion PreviewRegion(IRegion activeRegion, double sheetX, double sheetY)
    {
        AutofillDragCalculator
            .TryCalculatePreviewRegion(_sheet, activeRegion, _layoutProvider, sheetX, sheetY, out var region)
            .Should().BeTrue();
        region.Should().NotBeNull();
        return region!;
    }

    private static void ShouldBe(IRegion region, int top, int bottom, int left, int right)
    {
        region.Top.Should().Be(top);
        region.Bottom.Should().Be(bottom);
        region.Left.Should().Be(left);
        region.Right.Should().Be(right);
    }

    [Test]
    public void Drag_Right_Expands_Across_Columns()
    {
        var active = new Region(1, 1);
        var (x, y) = Centre(1, 4);

        ShouldBe(PreviewRegion(active, x, y), 1, 1, 1, 4);
    }

    [Test]
    public void Drag_Down_Expands_Across_Rows()
    {
        var active = new Region(1, 1);
        var (x, y) = Centre(6, 1);

        ShouldBe(PreviewRegion(active, x, y), 1, 6, 1, 1);
    }

    [Test]
    public void Drag_Back_Inside_The_Selection_Contracts_It()
    {
        var active = new Region(1, 5, 1, 5);
        // inside the selection, and further back in x than in y, so the columns contract
        var (x, y) = Centre(4, 2);

        ShouldBe(PreviewRegion(active, x, y), 1, 5, 1, 2);
    }

    [Test]
    public void Drag_Beyond_The_Sheet_Is_Clamped_To_It()
    {
        var active = new Region(1, 1);
        var (x, y) = Centre(1, 1);

        ShouldBe(PreviewRegion(active, x, y + 1000), 1, 9, 1, 1);
    }

    [Test]
    public void Movement_Inside_The_Dead_Zone_Is_Not_A_Drag()
    {
        var active = new Region(1, 1);
        var (handleX, handleY) = FillHandle(active);

        AutofillDragCalculator.TryCalculatePreviewRegion(_sheet, active, _layoutProvider,
                handleX + AutofillDragCalculator.DragThresholdInpx - 1,
                handleY + AutofillDragCalculator.DragThresholdInpx - 1,
                out _)
            .Should().BeFalse();

        AutofillDragCalculator.TryCalculatePreviewRegion(_sheet, active, _layoutProvider,
                handleX + AutofillDragCalculator.DragThresholdInpx,
                handleY,
                out _)
            .Should().BeTrue();
    }

    /// <summary>
    /// The drag is measured from the fill handle rather than from wherever the pointer went down, so
    /// the dead zone has to sit on the selection's bottom right corner however big the selection is.
    /// </summary>
    [Test]
    public void Dead_Zone_Is_Anchored_To_The_Fill_Handle_Of_A_Multi_Cell_Selection()
    {
        var active = new Region(2, 5, 3, 7);
        var (handleX, handleY) = FillHandle(active);

        AutofillDragCalculator
            .TryCalculatePreviewRegion(_sheet, active, _layoutProvider, handleX, handleY, out _)
            .Should().BeFalse();

        // a real drag away from the handle still resolves against absolute sheet coordinates
        var (x, y) = Centre(8, 7);
        ShouldBe(PreviewRegion(active, x, y), 2, 8, 3, 7);
    }
}
