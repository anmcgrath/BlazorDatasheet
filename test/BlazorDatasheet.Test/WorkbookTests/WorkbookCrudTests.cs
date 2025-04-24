using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
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

    [Test]
    public void Workbook_Events_Fire_On_Adding_Sheet()
    {
        var workbook = new Workbook();
        int addCount = 0;
        workbook.SheetAdded += (sender, args) => addCount++;
        workbook.AddSheet(10, 10);
        workbook.AddSheet("sheetName", new Sheet(10, 10));
        workbook.AddSheet("sheetName2", 1, 1);
        addCount.Should().Be(3);
    }

    [Test]
    public void Workbook_Events_Fire_On_Removeing_Sheet()
    {
        var workbook = new Workbook();
        workbook.AddSheet(10, 10);
        workbook.AddSheet(10, 10);
        workbook.AddSheet(10, 10);
        int removeCount = 0;
        workbook.SheetRemoved += (sender, args) => removeCount++;
        workbook.RemoveSheet("Sheet1");
        workbook.RemoveSheet("Sheet2");
        workbook.RemoveSheet("Sheet3");
        removeCount.Should().Be(3);
    }

    [Test]
    public void Workbook_Rename_Fire_On_Renaming_Sheet()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        WorkbookSheetRenamedEventArgs? a = null;
        workbook.SheetRenamed += (sender, args) => { a = args; };
        workbook.RenameSheet(sheet.Name, "NewName");
        a.Should().NotBeNull();
        a.NewName.Should().Be("NewName");
        a.OldName.Should().Be("Sheet1");
        a.Sheet.Should().Be(sheet);
    }
}