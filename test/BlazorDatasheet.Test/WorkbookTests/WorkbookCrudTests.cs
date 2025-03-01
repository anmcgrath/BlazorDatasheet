using System.Linq;
using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.WorkbookTests;

public class WorkbookCrudTests
{
    [Test]
    public void Single_Sheet_Has_Workbook_With_One_Sheet()
    {
        var sheet = new Sheet(10, 10);
        sheet.Workbook.Sheets.Should().HaveCount(1);
        sheet.Workbook.Sheets.First().Name.Should().Be(sheet.Name);
    }

    [Test]
    public void Add_New_Sheet_Adds_With_Correct_Sheet_Name()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet(10, 10);
        sheet1.Workbook.Should().BeSameAs(workbook);
        workbook.Sheets.Should().HaveCount(1);
        var sheet2 = workbook.AddSheet(10, 10);
        sheet2.Workbook.Should().BeSameAs(workbook);
        sheet2.Name.Should().Be("Sheet2");
    }

    [Test]
    public void Add_Sheet_With_Custom_Name_Sets_Sheet_Name()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Custom", 1, 1);
        sheet.Name.Should().Be("Custom");
    }

    [Test]
    public void Remove_Sheet_By_Name_Removes_Sheet()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet(1, 1);
        workbook.RemoveSheet(sheet1.Name);
        workbook.Sheets.Should().HaveCount(0);
    }

    [Test]
    public void Rename_Sheet_Removes_Sheet()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet(1, 1);
        workbook.RenameSheet(sheet1.Name, "NewName");
        workbook.Sheets.First().Name.Should().Be("NewName");
    }
}