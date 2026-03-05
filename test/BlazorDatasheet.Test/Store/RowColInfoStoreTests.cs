using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Store;

public class RowColInfoStoreTests
{
    [Test]
    public void Hidden_Row_Cols_Calculates_Correctly_When_Size_Is_one()
    {
        var sheet = new Sheet(100, 100);
        sheet.Rows.HideImpl(0, 100);
        sheet.Rows.CountVisible(0, 0).Should().Be(0);
        sheet.Rows.GetVisibleIndices(0, 0).Should().BeEmpty();
    }

    [Test]
    public void Hidden_Row_Cols_Calculate_Correctly_When_Size_Query_Larger_Than_Sheet()
    {
        var sheet = new Sheet(100, 100);
        sheet.Rows.HideImpl(0, 100);
        sheet.Rows.CountVisible(0, 1000).Should().Be(0);
    }

    [Test]
    public void Get_Visible_Indices_Fully_Out_Of_Range_Returns_Empty()
    {
        var sheet = new Sheet(100, 100);
        sheet.Rows.GetVisibleIndices(1000, 1001).Should().BeEmpty();
    }

    [Test]
    public void Count_Visible_Fully_Out_Of_Range_Returns_Zero()
    {
        var sheet = new Sheet(100, 100);
        sheet.Rows.CountVisible(1000, 1001).Should().Be(0);
    }

    [Test]
    public void Count_Visible_Partially_Out_Of_Range_Clamps_To_Sheet_Bounds()
    {
        var sheet = new Sheet(100, 100);
        sheet.Rows.Hide(5, 3);
        sheet.Rows.CountVisible(-50, 10).Should().Be(8);
    }

    [Test]
    public void Set_Size_On_Hidden_Row_Updates_Physical_Only_Until_Unhidden()
    {
        var sheet = new Sheet(20, 20);
        sheet.Rows.Hide(5, 1);
        sheet.Rows.SetSize(5, 123);

        sheet.Rows.IsVisible(5).Should().BeFalse();
        sheet.Rows.GetVisualHeight(5).Should().Be(0);
        sheet.Rows.GetPhysicalHeight(5).Should().Be(123);

        sheet.Rows.Unhide(5, 1);
        sheet.Rows.GetVisualHeight(5).Should().Be(123);
        sheet.Rows.GetPhysicalHeight(5).Should().Be(123);
    }

    [Test]
    public void Merged_Restore_Data_Restores_Physical_Sizes()
    {
        var sheet = new Sheet(20, 20);
        var restore1 = sheet.Rows.SetSizesImpl(2, 2, 50);
        var restore2 = sheet.Rows.SetSizesImpl(4, 4, 70);
        var mergedRestore = new RowColInfoRestoreData();
        mergedRestore.Merge(restore1);
        mergedRestore.Merge(restore2);

        sheet.Rows.GetPhysicalHeight(2).Should().Be(50);
        sheet.Rows.GetPhysicalHeight(4).Should().Be(70);

        sheet.Rows.Restore(mergedRestore);

        sheet.Rows.GetPhysicalHeight(2).Should().Be(sheet.Rows.DefaultSize);
        sheet.Rows.GetPhysicalHeight(4).Should().Be(sheet.Rows.DefaultSize);
    }
}
