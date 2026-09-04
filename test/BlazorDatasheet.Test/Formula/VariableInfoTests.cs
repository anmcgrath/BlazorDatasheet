using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class VariableInfoTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet(10, 10);
    }

    [Test]
    public void Value_Variable_Info_Is_Value_Kind()
    {
        _sheet.FormulaEngine.SetVariable("x", 5);
        var info = _sheet.FormulaEngine.GetVariableInfo("x");
        info.Should().NotBeNull();
        info!.Name.Should().Be("x");
        info.Kind.Should().Be(VariableKind.Value);
        info.Formula.Should().BeNull();
        info.References.Should().BeEmpty();
        info.IsRange.Should().BeFalse();
        info.Region.Should().BeNull();
        info.SheetName.Should().BeNull();
        info.ValueType.Should().Be(CellValueType.Number);
        info.Value.GetValue<double>().Should().Be(5);
    }

    [Test]
    public void Formula_Variable_Info_Reflects_Formula_And_Recalculation()
    {
        _sheet.Cells.SetValue(0, 0, 2);
        _sheet.FormulaEngine.SetVariable("y", "=Sheet1!A1*2");

        var info = _sheet.FormulaEngine.GetVariableInfo("y")!;
        info.Kind.Should().Be(VariableKind.Formula);
        info.Formula.Should().Be("=Sheet1!A1*2");
        info.References.Should().HaveCount(1);
        info.IsRange.Should().BeFalse();
        info.Value.GetValue<double>().Should().Be(4);

        _sheet.Cells.SetValue(0, 0, 3);
        _sheet.FormulaEngine.GetVariableInfo("y")!.Value.GetValue<double>().Should().Be(6);
    }

    [Test]
    public void Named_Range_Info_Is_Range_With_Region_And_Sheet()
    {
        _sheet.NamedRanges.Set("r", "A1:B2").Should().BeTrue();
        var info = _sheet.FormulaEngine.GetVariableInfo("r")!;
        info.Kind.Should().Be(VariableKind.Formula);
        info.IsRange.Should().BeTrue();
        info.Region.Should().BeEquivalentTo(new Region(0, 1, 0, 1));
        info.SheetName.Should().Be("Sheet1");
        _sheet.NamedRanges.GetNames().Should().BeEquivalentTo("r");
        _workbook.NamedRanges.IsNamedRange("r").Should().BeTrue();
    }

    [Test]
    public void GetVariableNames_And_VariableExists_Track_Set_And_Clear()
    {
        _sheet.FormulaEngine.SetVariable("x", 5);
        _sheet.FormulaEngine.SetVariable("y", "=Sheet1!A1*2");
        _sheet.NamedRanges.Set("r", "A1:B2");

        _sheet.FormulaEngine.GetVariableNames().Should().BeEquivalentTo("x", "y", "r");
        _sheet.FormulaEngine.GetVariableInfos().Select(v => v.Name).Should().BeEquivalentTo("x", "y", "r");
        _workbook.NamedRanges.GetNames().Should().BeEquivalentTo("r");
        _sheet.FormulaEngine.VariableExists("x").Should().BeTrue();
        _sheet.FormulaEngine.VariableExists("nope").Should().BeFalse();
        _sheet.FormulaEngine.GetVariableInfo("nope").Should().BeNull();

        _sheet.FormulaEngine.ClearVariable("x");
        _sheet.FormulaEngine.VariableExists("x").Should().BeFalse();
        _sheet.FormulaEngine.GetVariableNames().Should().BeEquivalentTo("y", "r");
    }

    [Test]
    public void NamedRanges_Clear_Leaves_Non_Range_Variables_Alone()
    {
        _sheet.FormulaEngine.SetVariable("x", 5);
        _sheet.NamedRanges.Clear("x");
        _sheet.FormulaEngine.VariableExists("x").Should().BeTrue();
    }

    [Test]
    public void Invalidated_Named_Range_Remains_Discoverable_And_Can_Be_Cleared()
    {
        _sheet.NamedRanges.Set("r", "A1");

        _sheet.Rows.RemoveAt(0);

        _sheet.NamedRanges.IsNamedRange("r").Should().BeTrue();
        _sheet.NamedRanges.GetNames().Should().Contain("r");
        _sheet.NamedRanges.GetRangeString("r").Should().Be("#REF!");

        _sheet.NamedRanges.Clear("r");
        _sheet.FormulaEngine.VariableExists("r").Should().BeFalse();
    }

    [Test]
    public void Variable_Info_Is_Detached_From_Engine()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.NamedRanges.Set("r", "A1:B2");
        var info = _sheet.FormulaEngine.GetVariableInfo("r")!;
        info.References[0].Shift(5, 5);
        info.Region!.Shift(5, 5);
        info.Value.GetValue<CellValue[][]>()![0][0] = CellValue.Number(99);

        var again = _sheet.FormulaEngine.GetVariableInfo("r")!;
        again.Formula.Should().Be("=Sheet1!A1:B2");
        again.Region.Should().BeEquivalentTo(new Region(0, 1, 0, 1));
        again.Value.GetValue<CellValue[][]>()![0][0].GetValue<double>().Should().Be(1);
        _sheet.NamedRanges.GetRangeString("r").Should().Be("Sheet1!A1:B2");
    }

    [Test]
    public void Named_Ranges_Are_Workbook_Wide()
    {
        var sheet2 = _workbook.AddSheet(10, 10);
        _sheet.NamedRanges.Set("r", "A1:B2");

        _workbook.NamedRanges.GetNames().Should().BeEquivalentTo("r");
        sheet2.NamedRanges.GetRangeString("r").Should().Be("Sheet1!A1:B2");
        sheet2.NamedRanges.GetNames().Should().BeEquivalentTo("r");

        // sheet-scoped views only list ranges on their own sheet and only match regions on their own sheet
        _sheet.NamedRanges.GetAll().Select(x => x.Name).Should().BeEquivalentTo("r");
        sheet2.NamedRanges.GetAll().Should().BeEmpty();
        _sheet.NamedRanges.GetRegionName(new Region(0, 1, 0, 1)).Should().Be("r");
        sheet2.NamedRanges.GetRegionName(new Region(0, 1, 0, 1)).Should().BeNull();

        // setting the same name from another sheet replaces it
        sheet2.NamedRanges.Set("r", "C3");
        _sheet.NamedRanges.GetRangeString("r").Should().Be("Sheet2!C3");
        _workbook.NamedRanges.GetNames().Should().BeEquivalentTo("r");
        _workbook.NamedRanges.GetAll().Single().SheetName.Should().Be("Sheet2");
    }

    [Test]
    public void Named_Range_Set_On_Workbook_Defaults_To_First_Sheet()
    {
        _workbook.NamedRanges.Set("r", "A1").Should().BeTrue();
        _workbook.NamedRanges.GetRangeString("r").Should().Be("Sheet1!A1");
        _workbook.NamedRanges.Set("bad", "A_1").Should().BeFalse();
        _workbook.NamedRanges.Set("1bad", "A1").Should().BeFalse();
    }

    [Test]
    public void Named_Ranges_Survive_Serialization_Round_Trip()
    {
        _sheet.NamedRanges.Set("r", "A1:B2");
        _sheet.FormulaEngine.SetVariable("x", 5);

        var json = new SheetJsonSerializer().Serialize(_workbook);
        var restored = new SheetJsonDeserializer().Deserialize(json);

        restored.NamedRanges.GetNames().Should().BeEquivalentTo("r");
        restored.NamedRanges.GetRangeString("r").Should().Be("Sheet1!A1:B2");
        restored.Sheets.First().NamedRanges.GetRegionName(new Region(0, 1, 0, 1)).Should().Be("r");
        restored.GetFormulaEngine().GetVariableInfo("x")!.Kind.Should().Be(VariableKind.Value);
    }
}
