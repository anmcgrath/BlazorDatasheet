using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class HideCommandTests
{
    [Test]
    public void Hide_Rows_Sets_height_to_zero_and_hides_Rows()
    {
        var sheet = new Sheet(100, 100);
        var cmd = new HideCommand(10, 18, Axis.Row);
        cmd.Execute(sheet);
        sheet.Rows.IsVisible(10).Should().BeFalse();
        sheet.Rows.IsVisible(15).Should().BeFalse();
        sheet.Rows.IsVisible(19).Should().BeTrue();
        sheet.Rows.GetVisualHeight(10).Should().Be(0);
        sheet.Rows.GetVisualHeight(15).Should().Be(0);
        sheet.Rows.GetVisualHeight(19).Should().Be(sheet.Rows.DefaultSize);

        sheet.Rows.GetNextVisible(9).Should().Be(19);

        cmd.Undo(sheet);
        sheet.Rows.IsVisible(10).Should().BeTrue();
        sheet.Rows.IsVisible(15).Should().BeTrue();
        sheet.Rows.IsVisible(19).Should().BeTrue();
        sheet.Rows.GetVisualHeight(10).Should().Be(sheet.Rows.DefaultSize);
    }
}