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
}