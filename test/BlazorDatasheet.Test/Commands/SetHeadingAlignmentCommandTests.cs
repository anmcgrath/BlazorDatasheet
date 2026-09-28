using System.Linq;
using AwesomeAssertions;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetHeadingAlignmentCommandTests
{
    [Test]
    public void SetHeadingAlignment_On_Column_Sets_Alignment_And_Undos()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.GetHeadingAlignment(2).Should().BeNull();

        sheet.Columns.SetHeadingAlignment(2, 2, TextAlign.Center);
        sheet.Columns.GetHeadingAlignment(2).Should().Be(TextAlign.Center);

        sheet.Commands.Undo();
        sheet.Columns.GetHeadingAlignment(2).Should().BeNull();

        sheet.Commands.Redo();
        sheet.Columns.GetHeadingAlignment(2).Should().Be(TextAlign.Center);
    }

    [Test]
    public void SetHeadingAlignment_Range_Sets_Multiple_Columns()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.SetHeadingAlignment(1, 3, TextAlign.End);

        sheet.Columns.GetHeadingAlignment(0).Should().BeNull();
        sheet.Columns.GetHeadingAlignment(1).Should().Be(TextAlign.End);
        sheet.Columns.GetHeadingAlignment(2).Should().Be(TextAlign.End);
        sheet.Columns.GetHeadingAlignment(3).Should().Be(TextAlign.End);
        sheet.Columns.GetHeadingAlignment(4).Should().BeNull();

        sheet.Commands.Undo();
        sheet.Columns.GetHeadingAlignment(1).Should().BeNull();
        sheet.Columns.GetHeadingAlignment(2).Should().BeNull();
        sheet.Columns.GetHeadingAlignment(3).Should().BeNull();
    }

    [Test]
    public void Clearing_HeadingAlignment_Removes_Sparse_Entries_And_Undos()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.SetHeadingAlignment(2, 4, TextAlign.Center);

        sheet.Columns.SetHeadingAlignment(1, 5, null);
        sheet.Columns.NonEmpty.Should().BeEmpty();

        sheet.Commands.Undo();
        sheet.Columns.NonEmpty.Select(col => col.ColIndex).Should().Equal(2, 3, 4);
        sheet.Commands.Redo();
        sheet.Columns.NonEmpty.Should().BeEmpty();
    }

    [Test]
    public void Row_HeadingAlignment_Is_Stored_And_Cleared()
    {
        var sheet = new Sheet(10, 10);
        sheet.Rows[2].HeadingAlignment = TextAlign.Start;

        sheet.Rows.NonEmpty.Select(row => row.RowIndex).Should().Equal(2);
        sheet.Rows[2].HeadingAlignment.Should().Be(TextAlign.Start);

        sheet.Rows[2].HeadingAlignment = null;
        sheet.Rows.NonEmpty.Should().BeEmpty();
        sheet.Commands.Undo();
        sheet.Rows[2].HeadingAlignment.Should().Be(TextAlign.Start);
    }

    [Test]
    public void SheetColumn_HeadingAlignment_Property_Works()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns[1].HeadingAlignment.Should().BeNull();

        sheet.Columns[1].HeadingAlignment = TextAlign.Center;
        sheet.Columns[1].HeadingAlignment.Should().Be(TextAlign.Center);
        sheet.Columns.GetHeadingAlignment(1).Should().Be(TextAlign.Center);

        sheet.Commands.Undo();
        sheet.Columns[1].HeadingAlignment.Should().BeNull();
    }

    [Test]
    public void Insert_Column_Shifts_HeadingAlignment()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.SetHeadingAlignment(2, TextAlign.Center);

        sheet.Columns.InsertAt(1, 2);
        sheet.Columns.GetHeadingAlignment(2).Should().BeNull();
        sheet.Columns.GetHeadingAlignment(4).Should().Be(TextAlign.Center);

        sheet.Commands.Undo();
        sheet.Columns.GetHeadingAlignment(2).Should().Be(TextAlign.Center);
    }

    [Test]
    public void Remove_Column_Shifts_HeadingAlignment()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.SetHeadingAlignment(3, TextAlign.Center);

        sheet.Columns.RemoveAt(1, 2);
        sheet.Columns.GetHeadingAlignment(1).Should().Be(TextAlign.Center);

        sheet.Commands.Undo();
        sheet.Columns.GetHeadingAlignment(3).Should().Be(TextAlign.Center);
    }

    [Test]
    public void HeadingTextAlign_On_ColumnInfoStore_Emits_HeadingsModified()
    {
        var sheet = new Sheet(10, 10);
        var eventFired = false;
        sheet.Columns.HeadingsModified += (_, _) => eventFired = true;

        sheet.Columns.HeadingTextAlign = TextAlign.Center;
        sheet.Columns.HeadingTextAlign.Should().Be(TextAlign.Center);
        eventFired.Should().BeTrue();
    }
}
