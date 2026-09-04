using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class ColumnGroupTests
{
    [Test]
    public void Set_Group_Then_Get_Group_Returns_Group_For_Each_Spanned_Column()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(1, 3, "Q1");

        sheet.Columns.HasGroups.Should().BeTrue();
        sheet.Columns.GetGroup(0).Should().BeNull();
        sheet.Columns.GetGroup(4).Should().BeNull();
        for (int col = 1; col <= 3; col++)
        {
            var g = sheet.Columns.GetGroup(col);
            g.Should().NotBeNull();
            g!.Start.Should().Be(1);
            g.End.Should().Be(3);
            g.Label.Should().Be("Q1");
        }
    }

    [Test]
    public void Overlapping_Set_Replaces_Existing_Group()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(0, 4, "A");
        sheet.Columns.SetGroup(2, 6, "B");

        var groups = sheet.Columns.GetGroups();
        groups.Should().HaveCount(2);
        groups[0].Should().Be(groups[0] with { Start = 0, End = 1 });
        groups[0].Label.Should().Be("A");
        groups[1].Start.Should().Be(2);
        groups[1].End.Should().Be(6);
        groups[1].Label.Should().Be("B");
    }

    [Test]
    public void Adjacent_Groups_With_Same_Label_Stay_Separate()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(0, 1, "Same");
        sheet.Columns.SetGroup(2, 3, "Same");

        sheet.Columns.GetGroups().Should().HaveCount(2);
    }

    [Test]
    public void Inserting_Columns_Inside_Group_Grows_It_And_Before_It_Shifts_It()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(2, 4, "G");

        sheet.Columns.InsertAt(3, 2);
        var g = sheet.Columns.GetGroups().Single();
        g.Start.Should().Be(2);
        g.End.Should().Be(6);

        sheet.Columns.InsertAt(0, 1);
        g = sheet.Columns.GetGroups().Single();
        g.Start.Should().Be(3);
        g.End.Should().Be(7);
    }

    [Test]
    public void Removing_Columns_Inside_Group_Shrinks_It_And_Removing_All_Deletes_It()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(2, 4, "G");

        sheet.Columns.RemoveAt(3, 1);
        var g = sheet.Columns.GetGroups().Single();
        g.Start.Should().Be(2);
        g.End.Should().Be(3);

        sheet.Columns.RemoveAt(2, 2);
        sheet.Columns.HasGroups.Should().BeFalse();
    }

    [Test]
    public void Setting_Group_Inside_Another_Splits_It_Into_Two_Groups_Sharing_A_Label()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(0, 4, "A");
        sheet.Columns.SetGroup(2, 2, "B");

        sheet.Columns.GetGroups().Select(g => (g.Start, g.End, g.Label))
            .Should().Equal((0, 1, "A"), (2, 2, "B"), (3, 4, "A"));
    }

    [Test]
    public void Undo_And_Redo_Restore_Groups()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(0, 2, "A");
        sheet.Columns.SetGroup(1, 3, "B");

        sheet.Commands.Undo();
        var g = sheet.Columns.GetGroups().Single();
        g.Start.Should().Be(0);
        g.End.Should().Be(2);
        g.Label.Should().Be("A");

        sheet.Commands.Undo();
        sheet.Columns.HasGroups.Should().BeFalse();

        sheet.Commands.Redo();
        sheet.Commands.Redo();
        sheet.Columns.GetGroups().Select(x => x.Label).Should().Equal("A", "B");
    }

    [Test]
    public void Clear_Groups_Removes_Overlapping_Groups_And_Is_Undoable()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(0, 1, "A");
        sheet.Columns.SetGroup(2, 3, "B");

        sheet.Columns.ClearGroups(1, 2);
        sheet.Columns.HasGroups.Should().BeFalse();

        sheet.Commands.Undo();
        sheet.Columns.GetGroups().Select(x => x.Label).Should().Equal("A", "B");
    }

    [Test]
    public void Total_Heading_Height_Includes_Group_Band_Only_When_Groups_Exist()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.HeadingHeight = 20;
        sheet.Columns.GroupHeadingHeight = 30;
        sheet.Columns.TotalHeadingHeight.Should().Be(20);

        var sizeModified = 0;
        sheet.Columns.SizeModified += (_, _) => sizeModified++;

        sheet.Columns.SetGroup(0, 1, "A");
        sheet.Columns.TotalHeadingHeight.Should().Be(50);
        sizeModified.Should().Be(1);

        sheet.Columns.SetGroup(3, 4, "B");
        sizeModified.Should().Be(1, "the band was already visible");

        sheet.Commands.Undo();
        sheet.Commands.Undo();
        sheet.Columns.TotalHeadingHeight.Should().Be(20);
        sizeModified.Should().Be(2);
    }

    [Test]
    public void Groups_Modified_Event_Fires_For_Column_Axis()
    {
        var sheet = new Sheet(5, 10);
        Axis? axis = null;
        sheet.Columns.GroupsModified += (_, e) => axis = e.Axis;
        sheet.Columns.SetGroup(0, 1, "A");
        axis.Should().Be(Axis.Col);
    }

    [Test]
    public void Structural_Changes_Emit_Group_And_Heading_Size_Events()
    {
        var sheet = new Sheet(5, 10);
        sheet.Columns.SetGroup(2, 4, "A");
        var groupsModified = 0;
        var sizeModified = 0;
        sheet.Columns.GroupsModified += (_, _) => groupsModified++;
        sheet.Columns.SizeModified += (_, _) => sizeModified++;

        sheet.Columns.InsertAt(0);
        groupsModified.Should().Be(1);
        sheet.Columns.GetGroups().Single().Start.Should().Be(3);

        sheet.Commands.Undo();
        groupsModified.Should().Be(2);
        sheet.Columns.GetGroups().Single().Start.Should().Be(2);

        sheet.Columns.RemoveAt(2, 3);
        groupsModified.Should().Be(3);
        sizeModified.Should().Be(1, "removing the final group hides the group heading band");
        sheet.Columns.HasGroups.Should().BeFalse();
    }
}
