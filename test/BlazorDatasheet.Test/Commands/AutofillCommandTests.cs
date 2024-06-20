using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Patterns;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class AutofillCommandTests
{
    [Test]
    public void Single_Number_Pattern_Extends()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Cells.SetValue(1, 0, 3);
        var cmd = new AutoFillCommand(
            new Region(0, 1, 0, 0),
            new Region(0, 3, 0, 0));
        cmd.Execute(sheet);
        sheet.Cells[2, 0].Value.Should().Be(5);
        sheet.Cells[3, 0].Value.Should().Be(7);
    }

    [Test]
    public void Shrink_Auto_Fill_Region_Removes_Data()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Cells.SetValue(1, 0, 3);
        var cmd = new AutoFillCommand(
            new Region(0, 1, 0, 0),
            new Region(0, 0, 0, 0));
        cmd.Execute(sheet);
        sheet.Cells[0, 0].Value.Should().Be(1);
        sheet.Cells[1, 0].ValueType.Should().Be(CellValueType.Empty);
    }

    [Test]
    public void Apply_Split_Number_Pattern_To_Fill_Data_Correctly()
    {
        IReadOnlyCell[] data =
        {
            new CellValueOnly(0, 0, 1d, CellValueType.Number),
            new CellValueOnly(1, 0, 3d, CellValueType.Number),
            new CellValueOnly(2, 0, "text1", CellValueType.Text),
            new CellValueOnly(3, 0, "text2", CellValueType.Text),
            new CellValueOnly(4, 0, 7d, CellValueType.Number),
            new CellValueOnly(5, 0, 8d, CellValueType.Number),
        };

        var sheet = new Sheet(100, 100);
        foreach (var cell in data)
            sheet.Cells.SetValue(cell.Row, cell.Col, cell.Value);

        var autoFillCommand = new AutoFillCommand(
            new Region(0, 5, 0, 0),
            new Region(0, 11, 0, 0));

        autoFillCommand.Execute(sheet);

        sheet.Cells[6, 0].Value.Should().Be(5);
        sheet.Cells[7, 0].Value.Should().Be(7);
        sheet.Cells[8, 0].Value.Should().Be("text1");
        sheet.Cells[9, 0].Value.Should().Be("text2");
        sheet.Cells[10, 0].Value.Should().Be(9);
        sheet.Cells[11, 0].Value.Should().Be(10);
    }

    [Test]
    public void Auto_Fill_With_Single_Number_Value_Repeats_Value()
    {
        double val = 2;
        var dataRegion = new Region(0, 10, 0, 0);
        var extendRegion = new Region(0, 10, 0, 2);

        var sheet = new Sheet(100, 100);
        foreach (var position in dataRegion)
            sheet.Cells[position.row, position.col].Value = val;

        var autoFill = new AutoFillCommand(dataRegion, extendRegion);
        autoFill.Execute(sheet);

        foreach (var position in extendRegion)
            sheet.Cells[position.row, position.col].Value.Should().Be(val);
    }

    [Test]
    public void Auto_Fill_With_Single_Number_And_Text_Performs_Regression()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells[2, 3].Value = 1;
        sheet.Cells[3, 3].Value = "text";

        var cmd = new AutoFillCommand(
            new Region(2, 3, 3, 3),
            new Region(2, 5, 3, 3));

        cmd.Execute(sheet);
        sheet.Cells[5, 3].Value.Should().Be("text");
        sheet.Cells[4, 3].Value.Should().Be(2);
    }

    [Test]
    public void Auto_Fill_Formula_Copies_And_Moves()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells[0, 0].Value = 2;
        sheet.Cells[0, 1].Value = 4;
        sheet.Cells[2, 3].Formula = "=SUM(A1:A2)";

        var cmd = new AutoFillCommand(
            new Region(2, 2, 3, 3),
            new Region(2, 2, 3, 4));

        cmd.Execute(sheet);
        sheet.Cells[2, 4].HasFormula().Should().Be(true);
        sheet.Cells[2, 4].Formula.Should().Be("=SUM(B1:B2)");

        sheet.Cells[0, 1].Value.Should().Be(4);

        cmd.Undo(sheet);
        sheet.Cells[2, 4].ValueType.Should().Be(CellValueType.Empty);
    }

    [Test]
    public void Auto_Fill_Cell_Format_Copies_Cell_Format()
    {
        var sheet = new Sheet(100, 100);
        var f = new CellFormat() { BackgroundColor = "f1" };
        sheet.SetFormat(new Region(0, 0), f);
        var cmd = new AutoFillCommand(new Region(0, 0), new Region(0, 1, 0, 0));
        cmd.Execute(sheet);
        sheet.GetFormat(1, 0).Should().BeEquivalentTo(f);
    }
}