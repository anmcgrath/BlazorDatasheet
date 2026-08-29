using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Filter;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Formats.DefaultConditionalFormats;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Core.Serialization.Json;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SerializationTests
{
    [Test]
    public void Sheet_Serialization_Should_Serialize()
    {
        Assert.DoesNotThrow(() =>
        {
            var wb = new Workbook();

            var sheet = wb.AddSheet("SheetName", 10, 10, 5, 6);
            sheet.Cells["A1"]!.Value = "Hello, World!";
            sheet.Cells["B2"]!.Value = 42;
            sheet.Cells["A3"]!.Value = 3.14;
            sheet.Cells["A4"]!.Value = true;
            sheet.Cells["A5"]!.Value = new DateTime(2021, 1, 1);
            sheet.Cells["A7"]!.Formula = "=sum(A2:A3)";
            sheet.Range("D:D")!.Type = "boolean";

            sheet.ConditionalFormats.Apply(new ColumnRegion(5),
                new NumberScaleConditionalFormat(Color.Aqua, Color.Red));
            sheet.ConditionalFormats.Apply(new ColumnRegion(6),
                new CustomCf("=A1=\"Hello, World!\"", Color.Red));

            sheet.Cells.SetCellMetaData(5, 5, "test1", "testData");
            sheet.Cells.SetCellMetaData(5, 5, "test2", 5);
            sheet.Cells.SetCellMetaData(5, 5, "test3", true);

            sheet.Range("F2:F3").Merge();


            var s = new SheetJsonSerializer();
            s.Resolvers.ConditionalFormat.Add(nameof(CustomCf), typeof(CustomCf));
            var json = s.Serialize(sheet.Workbook);

            var deserializer = new SheetJsonDeserializer();
            deserializer.Resolvers.ConditionalFormat.Add(nameof(CustomCf), typeof(CustomCf));
            var wbDeserialized = deserializer.Deserialize(json);
            CompareSheets(sheet.Workbook, wbDeserialized);
        });
    }

    [Test]
    public void Variables_Should_Be_Serialized()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"]!.Value = "TestA1";
        sheet.FormulaEngine.SetVariable("test", "=Sheet1!A1");
        sheet.FormulaEngine.SetVariable("test2", 10);
        sheet.FormulaEngine.SetVariable("test3", "=Sheet1!B1:B5");

        var s = new SheetJsonSerializer();
        var d = new SheetJsonDeserializer();

        var json = s.Serialize(sheet.Workbook, true);
        var workbook = d.Deserialize(json);

        var variables = workbook.Sheets.First().FormulaEngine.GetVariables().ToList();
        variables.Should().NotBeEmpty();
        var testVar = variables.First(x => x.Name == "test");
        var test2Var = variables.First(x => x.Name == "test2");
        var test3Var = variables.First(x => x.Name == "test3");

        testVar.Formula.Should().Be("=Sheet1!A1");
        test2Var.Value.GetValue<double>().Should().Be(10);
        test3Var.Formula.Should().Be("=Sheet1!B1:B5");
    }

    [Test]
    public void Multi_Stop_Number_Scale_Should_Be_Serialized()
    {
        var sheet = new Sheet(3, 1);
        sheet.Cells.SetValue(0, 0, 1);
        sheet.Cells.SetValue(1, 0, 2);
        sheet.Cells.SetValue(2, 0, 3);
        var cf = NumberScaleConditionalFormat.Viridis;
        sheet.ConditionalFormats.Apply(sheet.Region, cf);
        var originalBackground = sheet.ConditionalFormats.GetFormatResult(1, 0)!.BackgroundColor;

        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json);
        var deserializedSheet = deserialized.Sheets.First();
        var deserializedCf = deserializedSheet.ConditionalFormats.GetAllFormats().Single().Data
            .Should().BeOfType<NumberScaleConditionalFormat>().Subject;

        deserializedCf.ColorStops.Should().NotBeNull();
        deserializedCf.ColorStops!.Select(x => x.ToArgb()).Should()
            .Equal(cf.ColorStops!.Select(x => x.ToArgb()));
        deserializedCf.LutSize.Should().Be(40);
        deserializedSheet.ConditionalFormats.GetFormatResult(1, 0)!.BackgroundColor.Should().Be(originalBackground);
    }

    [Test]
    public void Colors_With_Alpha_Should_Round_Trip()
    {
        var sheet = new Sheet(1, 1);
        var color = Color.FromArgb(64, 10, 20, 30);
        sheet.ConditionalFormats.Apply(sheet.Region, new NumberScaleConditionalFormat(color, color));

        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json).Sheets.First();
        var conditionalFormat = deserialized.ConditionalFormats.GetAllFormats().Single().Data
            .Should().BeOfType<NumberScaleConditionalFormat>().Subject;

        conditionalFormat.ColorStart.ToArgb().Should().Be(color.ToArgb());
        conditionalFormat.ColorEnd.ToArgb().Should().Be(color.ToArgb());
    }

    [Test]
    public void Validators_Should_Be_Serialized()
    {
        var sheet = new Sheet(10, 10);
        sheet.Range("A1:A2")!.AddValidator(new SourceValidator(["a", "b"], false));
        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json);
        var dSheet = deserialized.Sheets.First();

        sheet.Validators.GetAll().Should().BeEquivalentTo(dSheet.Validators.GetAll());
    }

    [Test]
    public void Column_Fitlers_Should_Be_Serialized()
    {
        var sheet = new Sheet(10, 10);
        var patternFilter = new PatternFilter(PatternFilterType.Contains, "x");
        sheet.Columns.Filters.Set(5, patternFilter);
        var valueFilter = new ValueFilter();
        valueFilter.Exclude(CellValue.Number(10));
        valueFilter.Exclude(CellValue.Text("s"));
        sheet.Columns.Filters.Set(8, valueFilter);

        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var dSheet = new SheetJsonDeserializer().Deserialize(json).Sheets.First();

        dSheet.Columns.Filters.GetAll().Should().BeEquivalentTo(sheet.Columns.Filters.GetAll());
    }

    [Test]
    public void FrozenValsShouldBeSerialized()
    {
        var sheet = new Sheet(10, 10);
        sheet.FreezeRowCols(1, 2, 3, 4);
        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json);
        sheet.FreezeState.Should().BeEquivalentTo(deserialized.Sheets.First().FreezeState);
    }

    private void CompareSheets(Workbook wb1, Workbook wb2)
    {
        var sheets1 = wb1.Sheets.ToArray();
        var sheets2 = wb2.Sheets.ToArray();

        for (int sIndex = 0; sIndex < sheets1.Length; sIndex++)
        {
            var sheet1 = sheets1[sIndex];
            var sheet2 = sheets2[sIndex];

            sheet1.Name.Should().Be(sheet2.Name);
            sheet1.Rows.DefaultSize.Should().Be(sheet2.Rows.DefaultSize);
            sheet1.Columns.DefaultSize.Should().Be(sheet2.Columns.DefaultSize);

            sheet2.NumRows.Should().Be(sheet1.NumRows);
            sheet2.NumCols.Should().Be(sheet1.NumCols);
            sheet2.Rows.NonEmpty.Count().Should().Be(sheet1.Rows.NonEmpty.Count());
            sheet2.Columns.NonEmpty.Count().Should().Be(sheet1.Columns.NonEmpty.Count());
            sheet2.Cells.GetMerges(sheet1.Region).Should().BeEquivalentTo(sheet1.Cells.GetMerges(sheet1.Region));

            var rows1 = Enumerable.Range(0, sheet1.Rows.NonEmpty.Count()).Select(x => new SheetRow(x, sheet1))
                .ToArray();
            var rows2 = Enumerable.Range(0, sheet2.Rows.NonEmpty.Count()).Select(x => new SheetRow(x, sheet2))
                .ToArray();

            for (int rowIndex = 0; rowIndex < sheet1.NumRows; rowIndex++)
            {
                sheet1.Rows[rowIndex].RowIndex.Should().Be(sheet2.Rows[rowIndex].RowIndex);
                sheet1.Rows[rowIndex].Height.Should().Be(sheet2.Rows[rowIndex].Height);
                sheet1.Rows[rowIndex].IsVisible.Should().Be(sheet2.Rows[rowIndex].IsVisible);
                sheet1.Rows[rowIndex].Heading.Should().Be(sheet2.Rows[rowIndex].Heading);
                sheet1.Rows[rowIndex].NonEmptyCells.Count().Should().Be(sheet2.Rows[rowIndex].NonEmptyCells.Count());

                for (int colIndex = 0; colIndex < sheet1.NumCols; colIndex++)
                {
                    var cell1 = sheet1.Cells.GetCell(rowIndex, colIndex);
                    var cell2 = sheet2.Cells.GetCell(rowIndex, colIndex);

                    cell1.Col.Should().Be(cell2.Col);
                    cell1.CellValue.Should().BeEquivalentTo(cell2.CellValue);
                    cell1.Formula.Should().Be(cell2.Formula);
                    cell1.Format.Should().BeEquivalentTo(cell2.Format);
                    cell1.IsValid.Should().Be(cell2.IsValid);
                    cell1.ValueType.Should().Be(cell2.ValueType);
                    cell1.MetaData.Should().BeEquivalentTo(cell2.MetaData);
                    cell1.Type.Should().BeEquivalentTo(cell2.Type);
                }
            }
        }
    }

    [Test]
    public void Hidden_Rows_And_Cols_Deserialize_Correctly()
    {
        var sheet = new Sheet(10, 10, 10, 10);
        sheet.Rows.Hide(5, 2);
        sheet.Columns.Hide(5, 2);
        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var d = new SheetJsonDeserializer().Deserialize(json).Sheets.First();
        d.Rows.CountVisible(0, sheet.NumRows - 1).Should().Be(8);
        d.Columns.CountVisible(0, sheet.NumCols - 1).Should().Be(8);
        d.Rows.GetVisualHeight(5).Should().Be(0);
        d.Rows.GetVisualHeight(6).Should().Be(0);
        d.Columns.GetVisualWidth(5).Should().Be(0);
        d.Columns.GetVisualWidth(6).Should().Be(0);
    }

    [Test]
    public void Row_Col_Size_Should_Deserialize_Correctly()
    {
        var sheet = new Sheet(10, 10);
        sheet.Rows.SetSize(0, 5);
        sheet.Rows.SetSize(1, 7);
        sheet.Columns.SetSize(0, 6);
        sheet.Columns.SetSize(1, 8);
        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var d = new SheetJsonDeserializer().Deserialize(json).Sheets.First();
        d.Rows.GetPhysicalHeight(0).Should().Be(5);
        d.Rows.GetPhysicalHeight(1).Should().Be(7);
        d.Columns.GetPhysicalWidth(0).Should().Be(6);
        d.Columns.GetPhysicalWidth(1).Should().Be(8);
    }

    [Test]
    public void Fractional_Default_Row_And_Column_Sizes_Should_Round_Trip()
    {
        var sheet = new Sheet(2, 2, 105.75, 24.5);

        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json).Sheets.First();

        deserialized.Columns.DefaultSize.Should().Be(105.75);
        deserialized.Rows.DefaultSize.Should().Be(24.5);
    }

    [Test]
    public void Cell_Format_Null_Overrides_And_Unset_Properties_Should_Round_Trip()
    {
        var sheet = new Sheet(1, 1);
        sheet.Rows.Formats.Add(0, 0,
            new CellFormat { BackgroundColor = "red", TextWrap = TextWrapping.Wrap });
        sheet.SetFormat(new Region(0, 0),
            new CellFormat { BackgroundColor = null, ForegroundColor = "white" });

        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json).Sheets.First();
        var format = deserialized.Cells[0, 0]!.Format;

        format.BackgroundColor.Should().BeNull();
        format.ForegroundColor.Should().Be("white");
        format.TextWrap.Should().Be(TextWrapping.Wrap);
    }

    [Test]
    public void Metadata_Json_Values_Should_Deserialize_Recursively()
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells[0, 0]!.SetMetaData("integer", 5);
        sheet.Cells[0, 0]!.SetMetaData("nested", new Dictionary<string, object>
        {
            ["items"] = new object?[] { 1, "two", true, null }
        });

        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var deserialized = new SheetJsonDeserializer().Deserialize(json).Sheets.First();

        deserialized.Cells[0, 0]!.GetMetaData("integer").Should().BeOfType<int>().Which.Should().Be(5);
        var nested = deserialized.Cells[0, 0]!.GetMetaData("nested")
            .Should().BeOfType<Dictionary<string, object>>().Subject;
        var items = nested["items"].Should().BeOfType<List<object>>().Subject;
        items.Should().HaveCount(4);
        items[0].Should().Be(1);
        items[1].Should().Be("two");
        items[2].Should().Be(true);
        items[3].Should().BeNull();
    }

    [Test]
    public void Variables_Referencing_Variables_Should_Be_Deserialised_Correctly()
    {
        var sheet = new Sheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", "=y");
        sheet.FormulaEngine.SetVariable("y", CellValue.Number(2));
        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var d = new SheetJsonDeserializer().Deserialize(json).Sheets.First();
        d.FormulaEngine.TryGetVariable("x", out var x);
        d.FormulaEngine.TryGetVariable("y", out var y);
        x.Should().BeEquivalentTo(CellValue.Number(2));
        y.Should().BeEquivalentTo(CellValue.Number(2));
    }

    [Test]
    public void Cells_Referencing_Variables_Should_Be_Deserialised_Correctly()
    {
        var sheet = new Sheet(10, 10);
        sheet.FormulaEngine.SetVariable("x", CellValue.Number(2));
        sheet.Cells["A1"]!.Formula = "=x";
        var json = new SheetJsonSerializer().Serialize(sheet.Workbook);
        var d = new SheetJsonDeserializer().Deserialize(json).Sheets.First();
        d.Cells["A1"]!.Formula.Should().Be("=x");
        d.Cells["A1"]!.CellValue.Should().Be(CellValue.Number(2));

        d.FormulaEngine.SetVariable("x", CellValue.Number(3));
        d.Cells["A1"]!.CellValue.Should().Be(CellValue.Number(3));
    }

    [Test]
    public void Custom_Functions_Should_Be_Registered_Before_Formulas_Are_Deserialised()
    {
        var formulaOptions = new FormulaOptions
        {
            ConfigureFunctions = builder => builder.Add(new FunctionDescriptor(
                "DOUBLE",
                [new ParameterDefinition("value", ParameterType.Number)],
                (args, _) => CellValue.Number(args[0].GetValue<double>() * 2)))
        };
        var workbook = new Workbook(formulaOptions);
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells["A1"]!.Value = 2;
        sheet.Cells["B1"]!.Formula = "=DOUBLE(A1)";

        var json = new SheetJsonSerializer().Serialize(workbook);
        var deserialised = new SheetJsonDeserializer().Deserialize(json, formulaOptions);
        var deserialisedSheet = deserialised.Sheets.First();

        deserialisedSheet.Cells["B1"]!.Formula.Should().Be("=DOUBLE(A1)");
        deserialisedSheet.Cells["B1"]!.CellValue.Should().Be(CellValue.Number(4));

        deserialisedSheet.Cells["A1"]!.Value = 3;
        deserialisedSheet.Cells["B1"]!.CellValue.Should().Be(CellValue.Number(6));
    }

    [Test]
    public void Cross_Sheet_Formulas_Should_Be_Deserialised_With_Dependencies()
    {
        var workbook = new Workbook();
        var inputs = workbook.AddSheet("Inputs", 10, 10);
        var calculations = workbook.AddSheet("Calculations", 10, 10);
        inputs.Cells["A1"]!.Value = 2;
        calculations.Cells["A1"]!.Formula = "=Inputs!A1*2";

        var json = new SheetJsonSerializer().Serialize(workbook);
        var deserialised = new SheetJsonDeserializer().Deserialize(json);
        var deserialisedInputs = deserialised.GetSheet("Inputs")!;
        var deserialisedCalculations = deserialised.GetSheet("Calculations")!;

        deserialisedCalculations.Cells["A1"]!.CellValue.Should().Be(CellValue.Number(4));

        deserialisedInputs.Cells["A1"]!.Value = 3;
        deserialisedCalculations.Cells["A1"]!.CellValue.Should().Be(CellValue.Number(6));
    }

    [Test]
    public void Non_Finite_Doubles_Should_Be_Deserialised_Correctly()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells["A1"]!.Value = CellValue.Number(double.PositiveInfinity);
        var json = new SheetJsonSerializer().Serialize(workbook);
        var deserialised = new SheetJsonDeserializer().Deserialize(json);
        deserialised.Sheets.First().Cells["A1"]!.CellValue.Should()
            .Be(CellValue.Number(double.PositiveInfinity));
    }

    [Test]
    public void Non_Finite_Doubles_Should_Round_Trip_In_Cells_Variables_And_Collections()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells.SetValue(0, 0, CellValue.Number(double.NaN));
        sheet.Cells.SetValue(0, 1, CellValue.Number(double.NegativeInfinity));
        sheet.Cells.SetValue(0, 2, CellValue.Array([
            [CellValue.Number(double.PositiveInfinity), CellValue.Number(double.NaN)]
        ]));
        sheet.FormulaEngine.SetVariable("nonFiniteSequence", CellValue.Sequence([
            CellValue.Number(double.NegativeInfinity), CellValue.Number(double.NaN)
        ]));

        var json = new SheetJsonSerializer().Serialize(workbook);
        var deserialised = new SheetJsonDeserializer().Deserialize(json);
        var deserialisedSheet = deserialised.Sheets.First();

        double.IsNaN(deserialisedSheet.Cells["A1"]!.CellValue.NumberValue).Should().BeTrue();
        deserialisedSheet.Cells["B1"]!.CellValue.NumberValue.Should().Be(double.NegativeInfinity);

        var array = (CellValue[][])deserialisedSheet.Cells["C1"]!.CellValue.Data!;
        array[0][0].NumberValue.Should().Be(double.PositiveInfinity);
        double.IsNaN(array[0][1].NumberValue).Should().BeTrue();

        deserialised.GetFormulaEngine().TryGetVariable("nonFiniteSequence", out var sequence).Should().BeTrue();
        var sequenceValues = (CellValue[])sequence.Data!;
        sequenceValues[0].NumberValue.Should().Be(double.NegativeInfinity);
        double.IsNaN(sequenceValues[1].NumberValue).Should().BeTrue();
    }

    [Test]
    public void Errors_Arrays_And_Sequences_Should_Round_Trip()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells.SetValue(0, 0, CellValue.Error(ErrorType.Na, "No result"));
        sheet.Cells.SetValue(0, 1, CellValue.Array([
            [CellValue.Number(1), CellValue.Text("two")],
            [CellValue.Logical(true), CellValue.Error(ErrorType.Ref, "Missing range")]
        ]));
        sheet.FormulaEngine.SetVariable("sequence", CellValue.Sequence([
            CellValue.Date(new DateTime(2026, 8, 29)), CellValue.Error(ErrorType.Value, "Bad value")
        ]));

        var json = new SheetJsonSerializer().Serialize(workbook);
        var deserialised = new SheetJsonDeserializer().Deserialize(json);
        var deserialisedSheet = deserialised.Sheets.First();

        AssertError(deserialisedSheet.Cells["A1"]!.CellValue, ErrorType.Na, "No result");

        var array = (CellValue[][])deserialisedSheet.Cells["B1"]!.CellValue.Data!;
        array[0][0].Should().Be(CellValue.Number(1));
        array[0][1].Should().Be(CellValue.Text("two"));
        array[1][0].Should().Be(CellValue.Logical(true));
        AssertError(array[1][1], ErrorType.Ref, "Missing range");

        deserialised.GetFormulaEngine().TryGetVariable("sequence", out var sequence).Should().BeTrue();
        var sequenceValues = (CellValue[])sequence.Data!;
        sequenceValues[0].Should().Be(CellValue.Date(new DateTime(2026, 8, 29)));
        AssertError(sequenceValues[1], ErrorType.Value, "Bad value");
    }

    [Test]
    public void Formula_Cached_Values_Should_Not_Be_Serialized_And_Should_Be_Recalculated()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells["A1"]!.Value = 1;
        sheet.Cells["A2"]!.Value = 2;
        sheet.Cells["B1"]!.Formula = "=A1:A2";
        sheet.Cells["C1"]!.Formula = "=1/0";

        sheet.Cells["B1"]!.CellValue.ValueType.Should().Be(CellValueType.Array);
        sheet.Cells["C1"]!.CellValue.ValueType.Should().Be(CellValueType.Error);

        var json = new SheetJsonSerializer().Serialize(workbook);
        using var document = JsonDocument.Parse(json);
        var formulaCells = document.RootElement.GetProperty("Sheets")[0].GetProperty("Rows")[0]
            .GetProperty("Cells").EnumerateArray()
            .Where(cell => cell.TryGetProperty("Formula", out _))
            .ToArray();

        formulaCells.Should().HaveCount(2);
        formulaCells.All(cell =>
            !cell.TryGetProperty("Type", out _) && !cell.TryGetProperty("Data", out _)).Should().BeTrue();

        var deserialised = new SheetJsonDeserializer().Deserialize(json);
        var deserialisedSheet = deserialised.Sheets.First();
        deserialisedSheet.Cells["B1"]!.CellValue.ValueType.Should().Be(CellValueType.Array);
        deserialisedSheet.Cells["C1"]!.CellValue.ValueType.Should().Be(CellValueType.Error);

        deserialisedSheet.Cells["A1"]!.Value = 3;
        var recalculated = (CellValue[][])deserialisedSheet.Cells["B1"]!.CellValue.Data!;
        recalculated[0][0].Should().Be(CellValue.Number(3));
    }

    [Test]
    public void Reference_And_Unknown_Cell_Values_Should_Fail_Serialization_Clearly()
    {
        var unknownWorkbook = new Workbook();
        var unknownSheet = unknownWorkbook.AddSheet(10, 10);
        unknownSheet.Cells.SetValue(0, 0, new CellValue(new object()));

        var serializeUnknown = () => new SheetJsonSerializer().Serialize(unknownWorkbook);
        serializeUnknown.Should().Throw<NotSupportedException>().WithMessage("*Unknown*");

        var referenceWorkbook = new Workbook();
        var referenceSheet = referenceWorkbook.AddSheet(10, 10);
        referenceSheet.FormulaEngine.SetVariable("reference",
            CellValue.Reference(new CellReference(0, 0, false, false)));

        var serializeReference = () => new SheetJsonSerializer().Serialize(referenceWorkbook);
        serializeReference.Should().Throw<NotSupportedException>().WithMessage("*Reference*");
    }

    [Test]
    public void Incomplete_Or_Unsupported_Cell_Value_Envelopes_Should_Fail_Deserialization_Clearly()
    {
        const string incompleteJson =
            """{"Sheets":[{"Name":"Sheet1","Rows":[{"Row":0,"Cells":[{"Col":0,"Type":6}]}],"NumRows":1,"NumCols":1}]}""";
        const string unsupportedJson =
            """{"Sheets":[{"Name":"Sheet1","Rows":[{"Row":0,"Cells":[{"Col":0,"Type":5,"Data":"A1"}]}],"NumRows":1,"NumCols":1}]}""";

        var deserializeIncomplete = () => new SheetJsonDeserializer().Deserialize(incompleteJson);
        deserializeIncomplete.Should().Throw<JsonException>().WithMessage("*both Type and Data*");

        var deserializeUnsupported = () => new SheetJsonDeserializer().Deserialize(unsupportedJson);
        deserializeUnsupported.Should().Throw<JsonException>().WithMessage("*Reference*");
    }

    [Test]
    public void Unknown_Structured_Properties_Should_Be_Ignored_By_Custom_Converters()
    {
        const string json =
            """{"Sheets":[{"Name":"Sheet1","Rows":[{"Row":0,"Cells":[{"Col":0,"Extra":{"Nested":123},"Type":6,"Data":4}]}],"NumRows":1,"NumCols":1}]}""";

        var deserialized = new SheetJsonDeserializer().Deserialize(json).Sheets.First();

        deserialized.Cells[0, 0]!.CellValue.Should().Be(CellValue.Number(4));
    }

    [Test]
    public void Unregistered_Custom_Types_Should_Fail_Deserialization_Clearly()
    {
        var sheet = new Sheet(1, 1);
        sheet.ConditionalFormats.Apply(sheet.Region, new CustomCf("=TRUE", Color.Red));
        var serializer = new SheetJsonSerializer();
        serializer.Resolvers.ConditionalFormat.Add(nameof(CustomCf), typeof(CustomCf));
        var json = serializer.Serialize(sheet.Workbook);

        var deserialize = () => new SheetJsonDeserializer().Deserialize(json);

        deserialize.Should().Throw<JsonException>()
            .WithMessage("*CustomCf*conditional format resolver*");
    }

    [Test]
    public void Formula_With_Error_Should_Be_deserialised_Correctly()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet(10, 10);
        sheet.Cells["A1"]!.Formula = "=1/0";
        var json = new SheetJsonSerializer().Serialize(workbook);
        var deserialised = new SheetJsonDeserializer().Deserialize(json);
        deserialised.Sheets.First().Cells["A1"]!.Formula.Should().Be("=1/0");
    }

    private static void AssertError(CellValue value, ErrorType errorType, string message)
    {
        value.ValueType.Should().Be(CellValueType.Error);
        var error = (FormulaError)value.Data!;
        error.ErrorType.Should().Be(errorType);
        error.Message.Should().Be(message);
    }
}

public class CustomCf : ConditionalFormatAbstractBase
{
    public string Formula { get; set; }
    public Color ColorIfTrue { get; set; }

    public CustomCf(string formula, Color colorIfTrue)
    {
        Formula = formula;
        ColorIfTrue = colorIfTrue;
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cform = sheet.FormulaEngine.ParseFormula(Formula, sheet.Name);
        var value = sheet.FormulaEngine.Evaluate(cform);
        if (value.ValueType == CellValueType.Logical && value.GetValue<bool>())
        {
            return new CellFormat()
            {
                BackgroundColor = System.Drawing.ColorTranslator.ToHtml(ColorIfTrue)
            };
        }

        return null;
    }
}
