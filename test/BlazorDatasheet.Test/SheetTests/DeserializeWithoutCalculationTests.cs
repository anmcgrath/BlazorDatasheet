using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class DeserializeWithoutCalculationTests
{
    /// <summary>
    /// Two sheets, with formulas depending across them and on a named variable, so that both the
    /// per-sheet calculation that loading a sheet triggers and the final full pass have work to do.
    /// </summary>
    private static string BuildJson()
    {
        var workbook = new Workbook();
        var first = workbook.AddSheet("First", 10, 10);
        var second = workbook.AddSheet("Second", 10, 10);

        first.Cells.SetValue(0, 0, 2);
        first.Cells.SetValue(1, 0, 3);
        first.Cells.SetFormula(2, 0, "=A1+A2");
        // forwards to a sheet that has not been loaded yet when this one is populated
        first.Cells.SetFormula(3, 0, "=Second!A1*10");

        second.Cells.SetFormula(0, 0, "=First!A3");
        second.Cells.SetFormula(1, 0, "=SUM(First!A1:A2)");
        second.Cells.SetFormula(2, 0, "=factor*A1");

        workbook.GetFormulaEngine().SetVariable("factor", 4);

        return new SheetJsonSerializer().Serialize(workbook);
    }

    private static object?[] ValuesOf(Workbook workbook) =>
        workbook.Sheets
            .SelectMany(sheet => Enumerable.Range(0, 4).Select(row => sheet.Cells.GetValue(row, 0)))
            .ToArray();

    [Test]
    public void Deserialize_Without_Calculating_Then_Calculating_Matches_Deserialize_With_Calculating()
    {
        var json = BuildJson();

        var calculated = new SheetJsonDeserializer().Deserialize(json);
        var deferred = new SheetJsonDeserializer().Deserialize(json, calculate: false);

        deferred.GetFormulaEngine().CalculateSheet(true);

        ValuesOf(deferred).Should().Equal(ValuesOf(calculated));
        deferred.Sheets.First(x => x.Name == "First").Cells.GetValue(2, 0).Should().Be(5d);
        deferred.Sheets.First(x => x.Name == "Second").Cells.GetValue(2, 0).Should().Be(20d);
    }

    [Test]
    public void Deserialize_Without_Calculating_Leaves_Formula_Cells_Unevaluated()
    {
        var json = BuildJson();

        var deferred = new SheetJsonDeserializer().Deserialize(json, calculate: false);

        var first = deferred.Sheets.First(x => x.Name == "First");
        // the stored values are not written, but the formulas are
        first.Cells.GetFormulaString(2, 0).Should().Be("=A1+A2");
        first.Cells.GetValue(2, 0).Should().BeNull();
    }

    [Test]
    public void Reusing_A_Deserializer_Gives_The_Same_Result_As_A_Fresh_One()
    {
        var json = BuildJson();

        var deserializer = new SheetJsonDeserializer();
        var first = deserializer.Deserialize(json);
        var second = deserializer.Deserialize(json);

        ValuesOf(second).Should().Equal(ValuesOf(first));
    }
}
