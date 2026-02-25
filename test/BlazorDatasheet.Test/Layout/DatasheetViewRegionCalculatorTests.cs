using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Virtualise;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Layout;

public class DatasheetViewRegionCalculatorTests
{
    [Test]
    public void Constrained_ViewRegion_Is_Clamped_To_Sheet_Bounds()
    {
        var constrained = DatasheetViewRegionCalculator.GetConstrainedViewRegion(
            new Region(-5, 20, -10, 30),
            numRows: 10,
            numCols: 8);

        constrained.Top.Should().Be(0);
        constrained.Bottom.Should().Be(9);
        constrained.Left.Should().Be(0);
        constrained.Right.Should().Be(7);
    }

    [Test]
    public void Main_ViewRegion_Excludes_Frozen_Edges()
    {
        var viewRegion = new Region(2, 12, 3, 15);
        var main = DatasheetViewRegionCalculator.GetMainViewRegion(
            viewRegion,
            numRows: 20,
            numCols: 30,
            frozenTopCount: 4,
            frozenBottomCount: 3,
            frozenLeftCount: 5,
            frozenRightCount: 2);

        main.Top.Should().Be(4);
        main.Bottom.Should().Be(12);
        main.Left.Should().Be(5);
        main.Right.Should().Be(15);
    }

    [Test]
    public void Frozen_Right_Uses_Column_Count_When_Calculating_Start()
    {
        var main = new Region(5, 40, 2, 20);
        var frozenRight = DatasheetViewRegionCalculator.GetFrozenRightRegion(
            main,
            numCols: 37,
            frozenRightCount: 4);

        frozenRight.Left.Should().Be(33);
        frozenRight.Right.Should().Be(36);
    }

    [Test]
    public void Frozen_Bottom_Uses_Row_Count_When_Calculating_Start()
    {
        var constrained = new Region(10, 80, 5, 20);
        var frozenBottom = DatasheetViewRegionCalculator.GetFrozenBottomRegion(
            constrained,
            numRows: 101,
            frozenBottomCount: 6);

        frozenBottom.Top.Should().Be(95);
        frozenBottom.Bottom.Should().Be(100);
        frozenBottom.Left.Should().Be(5);
        frozenBottom.Right.Should().Be(20);
    }

    [Test]
    public void Main_ViewRegion_Stays_Inside_View_When_Frozen_Counts_Are_Too_Large()
    {
        var viewRegion = new Region(10, 12, 20, 22);
        var main = DatasheetViewRegionCalculator.GetMainViewRegion(
            viewRegion,
            numRows: 50,
            numCols: 50,
            frozenTopCount: 40,
            frozenBottomCount: 40,
            frozenLeftCount: 30,
            frozenRightCount: 30);

        main.Top.Should().BeGreaterOrEqualTo(viewRegion.Top);
        main.Bottom.Should().BeLessOrEqualTo(viewRegion.Bottom);
        main.Left.Should().BeGreaterOrEqualTo(viewRegion.Left);
        main.Right.Should().BeLessOrEqualTo(viewRegion.Right);
    }
}
