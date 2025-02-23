using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class MultiSheetTests
{
    [Test]
    public void Sheet_References_Other_Sheet_In_Workbook()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet(10, 10);
        var sheet2 = workbook.AddSheet(10, 10);
        sheet2.Cells["A2"]!.Value = "Test";
        sheet1.Cells["A1"]!.Formula = "=Sheet2!A2";
        sheet1.Cells["A1"]!.Value.Should().Be("Test");
    }
}