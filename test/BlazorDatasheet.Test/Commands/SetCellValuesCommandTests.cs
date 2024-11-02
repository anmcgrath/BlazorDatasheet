using System;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetCellValuesCommandTests
{
    [Test]
    public void Set_Cell_Values_Respects_Cell_Type()
    {
        var sheet = new Sheet(10, 10);
        sheet.Commands.ExecuteCommand(new SetCellValuesCommand(0, 0, [["2020-09-09"]]));
        // ensure the conversion happens without setting type first
        sheet.Cells.GetValue(0, 0).Should().BeOfType<DateTime>();
        sheet.Cells.SetType(0, 0, "text");
        // now since the type is "text" the conversion should not happen
        sheet.Commands.ExecuteCommand(new SetCellValuesCommand(0, 0, [["2020-09-09"]]));
        sheet.Cells.GetValue(0, 0).Should().BeOfType<string>();
    }

    [Test]
    public void Set_Cell_Value_Respects_Cell_Type()
    {
        var sheet = new Sheet(10, 10);
        sheet.Commands.ExecuteCommand(new SetCellValueCommand(0, 0, "2020-09-09"));
        // ensure the conversion happens without setting type first
        sheet.Cells.GetValue(0, 0).Should().BeOfType<DateTime>();
        sheet.Cells.SetType(0, 0, "text");
        // now since the type is "text" the conversion should not happen
        sheet.Commands.ExecuteCommand(new SetCellValueCommand(0, 0, "2020-09-09"));
        sheet.Cells.GetValue(0, 0).Should().BeOfType<string>();
    }
}