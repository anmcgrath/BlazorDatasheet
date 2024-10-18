using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class RowColVisibility
{
    [Test]
    public void Hide_Row_Hides_Row()
    {
        var sheet = new Sheet(10, 10);
        sheet.Rows.HideImpl(5, 1);
        sheet.Rows.IsVisible(5).Should().BeFalse();
        sheet.Rows.IsVisible(4).Should().BeTrue();
        sheet.Rows.IsVisible(6).Should().BeTrue();
    }

    [Test]
    public void Next_Visible_Row_Returns_Correct()
    {
        var sheet = new Sheet(11, 11);
        sheet.Rows.HideImpl(5, 2);
        sheet.Rows.HideImpl(8, 2);
        sheet.Rows.GetNextVisible(0).Should().Be(1);
        sheet.Rows.GetNextVisible(4).Should().Be(7);
        sheet.Rows.GetNextVisible(7).Should().Be(10);
        sheet.Rows.GetNextVisible(8).Should().Be(10);
        sheet.Rows.GetNextVisible(10).Should().Be(-1);
    }

    [Test]
    public void Next_Visible_Row_Inside_Hidden_Region_returns_Correct()
    {
        var sheet = new Sheet(11, 11);
        sheet.Rows.Hide(0, 5);
        sheet.Rows.GetNextVisible(1).Should().Be(5);
        sheet.Rows.GetNextVisible(0).Should().Be(5);
    }

    [Test]
    public void Next_Visible_With_Hidden_Surrounding_Returns_Correct()
    {
        var sheet = new Sheet(11, 11);
        sheet.Rows.Hide(0, 5);
        sheet.Rows.Hide(6, 2);
        sheet.Rows.GetNextVisible(5).Should().Be(8);
    }

    [Test]
    public void Next_Visible_With_Hidden_Surrounding_With_Size_1_Returns_Correct()
    {
        var sheet = new Sheet(500, 11);
        sheet.Rows.Hide(11, 1);
        sheet.Rows.Hide(13, 2);
        sheet.Rows.GetNextVisible(12).Should().Be(15);
    }

    [Test]
    public void Get_Visible_Row_Indices_Tests_Start_Row_Hidden()
    {
        /*
         * 0 H
         * 1 V
         * 2 V
         * 3 H
         * 4 H
         * 5 V
         */
        var sheet = new Sheet(6, 5);
        sheet.Rows.Hide(0, 1);
        sheet.Rows.Hide(3, 2);
        sheet.Rows.GetVisibleIndices(0, 5)
            .Should()
            .BeEquivalentTo([1, 2, 5]);
    }

    [Test]
    public void Get_Visible_Row_Indices_Tests_End_Row_Hidden()
    {
        /*
         * 0 V
         * 1 V
         * 2 V
         * 3 H
         * 4 V
         * 5 H
         */
        var sheet = new Sheet(6, 5);
        sheet.Rows.Hide(3, 1);
        sheet.Rows.Hide(5, 1);
        sheet.Rows.GetVisibleIndices(0, 5)
            .Should()
            .BeEquivalentTo([0, 1, 2, 4]);
    }

    [Test]
    public void Get_Visible_Row_Indices_One_Row_Hidden_Returns_Correct()
    {
        /*
         * 0 V
         * 1 V
         * 2 V
         * 3 V
         * 4 V
         * 5 H
         */
        var sheet = new Sheet(6, 5);
        sheet.Rows.Hide(5, 1);
        sheet.Rows.GetVisibleIndices(0, 5)
            .Should()
            .BeEquivalentTo([0, 1, 2, 3, 4]);
    }

    [Test]
    public void Get_Visible_Row_Indices_All_Rows_Hidden_Returns_Correct()
    {
        /*
         * 0 V
         * 1 V
         * 2 V
         * 3 V
         * 4 V
         * 5 H
         */
        var sheet = new Sheet(6, 5);
        sheet.Rows.Hide(0, 6);
        sheet.Rows.GetVisibleIndices(0, 5)
            .Should()
            .BeEmpty();
    }
}