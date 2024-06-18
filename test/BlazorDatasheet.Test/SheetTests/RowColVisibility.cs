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
        sheet.Rows.HideRowsImpl(5,1);
        sheet.Rows.IsRowVisible(5).Should().BeFalse();
        sheet.Rows.IsRowVisible(4).Should().BeTrue();
        sheet.Rows.IsRowVisible(6).Should().BeTrue();
    }
    
    [Test]
    public void Next_Visible_Row_Returns_Correct()
    {
        var sheet = new Sheet(11, 11);
        sheet.Rows.HideRowsImpl(5,2);
        sheet.Rows.HideRowsImpl(8,2);
        sheet.Rows.GetNextVisibleRow(0).Should().Be(1);
        sheet.Rows.GetNextVisibleRow(4).Should().Be(7);
        sheet.Rows.GetNextVisibleRow(7).Should().Be(10);
        sheet.Rows.GetNextVisibleRow(8).Should().Be(10);
        sheet.Rows.GetNextVisibleRow(10).Should().Be(-1);
    }
    
}