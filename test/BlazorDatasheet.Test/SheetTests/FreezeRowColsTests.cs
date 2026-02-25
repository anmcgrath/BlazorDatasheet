using System;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Layout;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class FreezeRowColsTests
{
    [Test]
    public void New_Sheet_Has_No_Frozen_Rows_Or_Columns()
    {
        var sheet = new Sheet(10, 10);

        sheet.FreezeState.Top.Should().Be(0);
        sheet.FreezeState.Bottom.Should().Be(0);
        sheet.FreezeState.Left.Should().Be(0);
        sheet.FreezeState.Right.Should().Be(0);
    }

    [Test]
    public void FreezeRowCols_Updates_State_And_Supports_Undo()
    {
        var sheet = new Sheet(10, 10);

        sheet.FreezeRowCols(1, 2, 3, 1);

        sheet.FreezeState.Should().BeEquivalentTo(new
        {
            Top = 1,
            Bottom = 2,
            Left = 3,
            Right = 1
        });

        sheet.Commands.Undo();

        sheet.FreezeState.Should().BeEquivalentTo(new
        {
            Top = 0,
            Bottom = 0,
            Left = 0,
            Right = 0
        });
    }

    [Test]
    public void FreezeRowCols_Raises_Event_With_Old_And_New_State()
    {
        var sheet = new Sheet(10, 10);
        SheetFrozenRowColsEventArgs? eventArgs = null;

        sheet.RowColsFrozen += (_, args) => eventArgs = args;

        sheet.FreezeRowCols(2, 1, 1, 2);

        eventArgs.Should().NotBeNull();
        eventArgs!.OldFreezeState.Should().BeEquivalentTo(new
        {
            Top = 0,
            Bottom = 0,
            Left = 0,
            Right = 0
        });
        eventArgs.NewFreezeState.Should().BeEquivalentTo(new
        {
            Top = 2,
            Bottom = 1,
            Left = 1,
            Right = 2
        });
    }

    [Test]
    public void Freeze_Helper_Methods_Update_Expected_Sides()
    {
        var sheet = new Sheet(20, 20);

        sheet.FreezeTopRows(2);
        sheet.FreezeBottomRows(3);
        sheet.FreezeLeftColumns(4);
        sheet.FreezeRightColumns(5);

        sheet.FreezeState.Should().BeEquivalentTo(new
        {
            Top = 2,
            Bottom = 3,
            Left = 4,
            Right = 5
        });
    }

    [Test]
    [TestCase(-1, 0, 0, 0)]
    [TestCase(0, -1, 0, 0)]
    [TestCase(0, 0, -1, 0)]
    [TestCase(0, 0, 0, -1)]
    [TestCase(11, 0, 0, 0)]
    [TestCase(0, 11, 0, 0)]
    [TestCase(0, 0, 11, 0)]
    [TestCase(0, 0, 0, 11)]
    [TestCase(4, 7, 0, 0)]
    [TestCase(0, 0, 4, 7)]
    public void FreezeRowCols_Invalid_Inputs_Throw(int top, int bottom, int left, int right)
    {
        var sheet = new Sheet(10, 10);

        Action act = () => sheet.FreezeRowCols(top, bottom, left, right);

        act.Should().Throw<ArgumentException>();
    }
}
