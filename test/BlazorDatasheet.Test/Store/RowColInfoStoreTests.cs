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
}