using System;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Core.Validation;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.WorkbookTests;

public class WorkbookDuplicateSheetTests
{
    [Test]
    public void Duplicate_Sheet_Takes_Next_Free_Name_When_None_Given()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Data", 5, 5);

        var copy = workbook.DuplicateSheet("Data");

        copy.Name.Should().Be("Sheet1");
        workbook.Sheets.Should().HaveCount(2);
        copy.Workbook.Should().BeSameAs(workbook);
    }

    [Test]
    public void Duplicate_Sheet_Copies_Values_And_Formulas()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Data", 5, 5);
        sheet.Cells[0, 0].Value = 3;
        sheet.Cells.SetFormula(1, 0, "=A1*2");

        var copy = workbook.DuplicateSheet("Data", "Copy");

        copy.Cells[0, 0].Value.Should().Be(3);
        copy.Cells.GetFormulaString(1, 0).Should().Be("=A1*2");
        copy.Cells[1, 0].Value.Should().Be(6);

        // The copy's unqualified references are its own: the source is unaffected by an edit.
        copy.Cells[0, 0].Value = 10;
        copy.Cells[1, 0].Value.Should().Be(20);
        sheet.Cells[1, 0].Value.Should().Be(6);
    }

    [Test]
    public void Duplicate_Sheet_Copies_Metadata_Formats_And_Merges()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Data", 5, 5);
        sheet.Cells.SetCellMetaData(0, 0, "marker", "kept");
        sheet.SetFormat(sheet.Range(0, 0).Region, new CellFormat { BackgroundColor = "#ff0000" });
        sheet.Cells.Merge(sheet.Range(2, 3, 0, 1).Region);
        sheet.Columns.SetSize(1, 250);

        var copy = workbook.DuplicateSheet("Data", "Copy");

        copy.Cells.GetMetaData(0, 0, "marker").Should().Be("kept");
        copy.GetFormat(0, 0)!.BackgroundColor.Should().Be("#ff0000");
        copy.Cells.IsInsideMerge(2, 0).Should().BeTrue();
        copy.Columns.GetPhysicalWidth(1).Should().Be(250);
    }

    [Test]
    public void Duplicate_Sheet_Gives_The_Copy_Its_Own_Conditional_Format_Rules()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Data", 5, 5);
        sheet.ConditionalFormats.Apply(sheet.Range(0, 0),
            new FormulaConditionalFormat("=A1>1", new CellFormat { BackgroundColor = "#00ff00" }));

        var copy = workbook.DuplicateSheet("Data", "Copy");

        var copied = copy.ConditionalFormats.GetAllFormats().Single();
        var original = sheet.ConditionalFormats.GetAllFormats().Single();
        copied.Data.Should().NotBeSameAs(original.Data);

        copy.Cells[0, 0].Value = 5;
        copy.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor.Should().Be("#00ff00");
        sheet.ConditionalFormats.GetFormatResult(0, 0)?.BackgroundColor.Should().NotBe("#00ff00");
    }

    [Test]
    public void Duplicate_Sheet_Copies_Validators()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Data", 5, 5);
        sheet.Validators.Add(sheet.Range(0, 0).Region, new NumberValidator(true));

        var copy = workbook.DuplicateSheet("Data", "Copy");

        copy.Validators.Get(0, 0).Should().HaveCount(1);
    }

    [Test]
    public void Duplicate_Sheet_Rejects_A_Name_Already_In_Use_And_An_Unknown_Source()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Data", 5, 5);
        workbook.AddSheet("Other", 5, 5);

        workbook.Invoking(w => w.DuplicateSheet("Data", "Other")).Should().Throw<Exception>();
        workbook.Invoking(w => w.DuplicateSheet("Missing")).Should().Throw<Exception>();
        workbook.Sheets.Should().HaveCount(2);
    }
}
