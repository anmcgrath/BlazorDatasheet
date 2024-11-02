using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class CellDataTests
{
    [Test]
    public void Can_Change_Implicit_Cell_Value_Conversion()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.BeforeCellValueConversion += (_, value) =>
        {
            value.NewValue = CellValue.Text("Intercepted");
        };
        sheet.Cells.SetValue(0, 0, "Test");
        sheet.Cells.GetValue(0, 0).Should().Be("Intercepted");
    }
}