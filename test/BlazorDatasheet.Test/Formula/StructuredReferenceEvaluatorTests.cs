using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatashet.Formula.Functions.Math;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class StructuredReferenceEvaluatorTests
{
    private const string Sheet = "Sheet1";
    private Evaluator _evaluator;
    private TestEnvironment _env;
    private Parser _parser;

    [SetUp]
    public void Setup()
    {
        _env = new TestEnvironment();
        _env.RegisterFunction(SumFunction.Descriptor);
        _evaluator = new Evaluator(_env);
        _parser = new Parser(_env);
    }

    /// <summary>
    /// Sales over A1:C4 (A1:C5 with totals): headers Qty/Price/Amount, three data rows and an optional
    /// totals row whose values would change any sum that included them.
    /// </summary>
    private void AddSalesTable(bool showTotals = false)
    {
        _env.SetCellValue(0, 0, "Qty");
        _env.SetCellValue(0, 1, "Price");
        _env.SetCellValue(0, 2, "Amount");
        for (var i = 1; i <= 3; i++)
        {
            _env.SetCellValue(i, 0, i);
            _env.SetCellValue(i, 1, i * 10);
            _env.SetCellValue(i, 2, i * i * 10);
        }

        if (showTotals)
        {
            _env.SetCellValue(4, 0, 1000);
            _env.SetCellValue(4, 1, 1000);
            _env.SetCellValue(4, 2, 1000);
        }

        _env.AddTable("Sales", Sheet, new Region(0, showTotals ? 4 : 3, 0, 2), showTotals);
    }

    private CellValue Eval(string formula, int? callerRow = null, int? callerCol = null)
    {
        FormulaCallerInfo? caller = callerRow == null ? null : new FormulaCallerInfo(callerRow.Value, callerCol!.Value, Sheet);
        return _evaluator.Evaluate(_parser.FromString(formula), caller: caller);
    }

    private Reference EvalReference(string formula, int? callerRow = null, int? callerCol = null)
    {
        FormulaCallerInfo? caller = callerRow == null ? null : new FormulaCallerInfo(callerRow.Value, callerCol!.Value, Sheet);
        var result = _evaluator.Evaluate(_parser.FromString(formula), options: new FormulaEvaluationOptions(true),
            caller: caller);
        result.ValueType.Should().Be(CellValueType.Reference);
        return result.GetValue<Reference>()!;
    }

    private static void ShouldBeError(CellValue value, ErrorType errorType)
    {
        value.IsError().Should().BeTrue();
        ((FormulaError)value.Data!).ErrorType.Should().Be(errorType);
    }

    private static void ShouldBeRegion(Reference reference, int r0, int r1, int c0, int c1)
    {
        reference.Region.Should().BeEquivalentTo(new Region(r0, r1, c0, c1));
        reference.SheetName.Should().Be(Sheet);
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public void Sum_Of_Column_Sums_Data_Rows_Only(bool showTotals)
    {
        AddSalesTable(showTotals);
        Eval("=SUM(Sales[Amount])").GetValue<double>().Should().Be(140);
        Eval("=SUM(Sales[Qty])").GetValue<double>().Should().Be(6);
    }

    [Test]
    public void Column_Reference_Resolves_To_Data_Rows_Of_Column()
    {
        AddSalesTable(showTotals: true);
        var reference = EvalReference("=Sales[Price]");
        reference.Should().BeOfType<RangeReference>();
        ShouldBeRegion(reference, 1, 3, 1, 1);
    }

    [Test]
    public void Column_Reference_Without_Resolving_Is_Array_Of_Values()
    {
        AddSalesTable();
        var result = Eval("=Sales[Qty]");
        result.IsArray().Should().BeTrue();
        result.GetValue<CellValue[][]>()!.Select(r => r[0].GetValue<double>()).Should().Equal(1, 2, 3);
    }

    [Test]
    [TestCase("=Sales[#All]", 0, 4, 0, 2)]
    [TestCase("=Sales[#Data]", 1, 3, 0, 2)]
    [TestCase("=Sales[]", 1, 3, 0, 2)]
    [TestCase("=Sales[#Headers]", 0, 0, 0, 2)]
    [TestCase("=Sales[#Totals]", 4, 4, 0, 2)]
    [TestCase("=Sales[[#Headers],[#Data]]", 0, 3, 0, 2)]
    [TestCase("=Sales[[#Data],[#Totals]]", 1, 4, 0, 2)]
    [TestCase("=Sales[[#All],[Price]]", 0, 4, 1, 1)]
    [TestCase("=Sales[[#Data],[#Totals],[Amount]]", 1, 4, 2, 2)]
    [TestCase("=Sales[[Qty]:[Price]]", 1, 3, 0, 1)]
    [TestCase("=Sales[[Price]:[Qty]]", 1, 3, 0, 1)]
    [TestCase("=Sales[[#Headers],[#Data],[Price]:[Amount]]", 0, 3, 1, 2)]
    public void Specifiers_Resolve_To_Regions_Of_A_Table_With_Totals(string formula, int r0, int r1, int c0, int c1)
    {
        AddSalesTable(showTotals: true);
        ShouldBeRegion(EvalReference(formula), r0, r1, c0, c1);
    }

    [Test]
    [TestCase("=Sales[#All]", 0, 3, 0, 2)]
    [TestCase("=Sales[[#Data],[#Totals]]", 1, 3, 0, 2)]
    public void Specifiers_Resolve_To_Regions_Of_A_Table_Without_Totals(string formula, int r0, int r1, int c0, int c1)
    {
        AddSalesTable();
        ShouldBeRegion(EvalReference(formula), r0, r1, c0, c1);
    }

    [Test]
    public void Sum_Of_Data_Excludes_Headers_And_Totals()
    {
        AddSalesTable(showTotals: true);
        Eval("=SUM(Sales[#Data])").GetValue<double>().Should().Be(6 + 60 + 140);
        Eval("=SUM(Sales[#All])").GetValue<double>().Should().Be(6 + 60 + 140 + 3000);
    }

    [Test]
    public void Header_Cell_Resolves_To_A_Cell_Reference()
    {
        AddSalesTable();
        var reference = EvalReference("=Sales[[#Headers],[Qty]]");
        reference.Should().BeOfType<CellReference>();
        ShouldBeRegion(reference, 0, 0, 0, 0);
        Eval("=Sales[[#Headers],[Price]]").GetValue<string>().Should().Be("Price");
    }

    [Test]
    public void This_Row_In_A_Data_Row_Is_The_Value_In_That_Row()
    {
        AddSalesTable();
        Eval("=Sales[@Qty]", 2, 5).GetValue<double>().Should().Be(2);
        Eval("=[@Qty]*[@Price]", 3, 1).GetValue<double>().Should().Be(90);
        Eval("=Sales[@Amount]", 1, 2).GetValue<double>().Should().Be(10);
    }

    [Test]
    public void This_Row_Without_Columns_Is_The_Whole_Row()
    {
        AddSalesTable();
        ShouldBeRegion(EvalReference("=Sales[@]", 2, 4), 2, 2, 0, 2);
        ShouldBeRegion(EvalReference("=[@[Qty]:[Price]]", 2, 2), 2, 2, 0, 1);
    }

    [Test]
    public void This_Row_In_The_Header_Or_Totals_Row_Is_Value_Error()
    {
        AddSalesTable(showTotals: true);
        ShouldBeError(Eval("=Sales[@Qty]", 0, 0), ErrorType.Value);
        ShouldBeError(Eval("=[@Qty]", 0, 1), ErrorType.Value);
        ShouldBeError(Eval("=Sales[@Qty]", 4, 0), ErrorType.Value);
        ShouldBeError(Eval("=Sales[@Qty]", 10, 0), ErrorType.Value);
    }

    [Test]
    public void This_Row_On_Another_Sheet_Is_Value_Error()
    {
        AddSalesTable();
        var result = _evaluator.Evaluate(_parser.FromString("=Sales[@Qty]"),
            caller: new FormulaCallerInfo(1, 0, "Sheet2"));
        ShouldBeError(result, ErrorType.Value);
    }

    [Test]
    public void This_Row_Without_A_Caller_Is_Value_Error()
    {
        AddSalesTable();
        ShouldBeError(Eval("=Sales[@Qty]"), ErrorType.Value);
        ShouldBeError(Eval("=[@Qty]"), ErrorType.Value);
    }

    [Test]
    [TestCase("=Missing[Qty]")]
    [TestCase("=SUM(Missing[Qty])")]
    [TestCase("=Sales[Missing]")]
    [TestCase("=Sales[[Qty]:[Missing]]")]
    [TestCase("=Sales[@Missing]")]
    public void Unknown_Table_Or_Column_Is_Ref_Error(string formula)
    {
        AddSalesTable();
        ShouldBeError(Eval(formula, 1, 0), ErrorType.Ref);
    }

    [Test]
    public void Totals_Without_A_Totals_Row_Is_Ref_Error()
    {
        AddSalesTable();
        ShouldBeError(Eval("=Sales[#Totals]"), ErrorType.Ref);
        ShouldBeError(Eval("=Sales[[#Totals],[Qty]]"), ErrorType.Ref);
    }

    [Test]
    public void Names_Are_Case_Insensitive()
    {
        AddSalesTable();
        Eval("=SUM(sales[amount])").GetValue<double>().Should().Be(140);
        Eval("=[@QTY]", 1, 0).GetValue<double>().Should().Be(1);
    }

    [Test]
    public void Unqualified_Reference_Resolves_Against_The_Table_Containing_The_Caller()
    {
        AddSalesTable();
        // header row and data rows are both inside the table
        Eval("=SUM([Amount])", 0, 2).GetValue<double>().Should().Be(140);
        Eval("=SUM([Amount])", 3, 0).GetValue<double>().Should().Be(140);
        Eval("=[@Amount]", 2, 0).GetValue<double>().Should().Be(40);
    }

    [Test]
    public void Unqualified_Reference_Outside_A_Table_Is_Ref_Error()
    {
        AddSalesTable();
        ShouldBeError(Eval("=SUM([Amount])", 5, 0), ErrorType.Ref);
        ShouldBeError(Eval("=[@Amount]", 2, 3), ErrorType.Ref);
        ShouldBeError(Eval("=SUM([Amount])"), ErrorType.Ref);
    }

    [Test]
    public void Unqualified_Reference_Picks_The_Right_Table()
    {
        AddSalesTable();
        _env.SetCellValue(0, 5, "Amount");
        _env.SetCellValue(1, 5, 7);
        _env.AddTable("Other", Sheet, new Region(0, 1, 5, 5));
        Eval("=SUM([Amount])", 1, 5).GetValue<double>().Should().Be(7);
        Eval("=SUM([Amount])", 1, 0).GetValue<double>().Should().Be(140);
    }

    [Test]
    public void Duplicate_And_Empty_Header_Names_Are_Made_Unique()
    {
        _env.SetCellValue(0, 0, "A");
        _env.SetCellValue(0, 1, "a");
        _env.AddTable("T", Sheet, new Region(0, 2, 0, 2));
        ShouldBeRegion(EvalReference("=T[A]"), 1, 2, 0, 0);
        ShouldBeRegion(EvalReference("=T[a2]"), 1, 2, 1, 1);
        ShouldBeRegion(EvalReference("=T[Column3]"), 1, 2, 2, 2);
    }

    [Test]
    public void Table_Names_Are_Listed()
    {
        AddSalesTable();
        _env.GetTableNames().Should().Equal("Sales");
    }

    [Test]
    public void Sheet_Table_Headers_Are_The_Headings()
    {
        _env.AddSheetTable("Data", Sheet, ["Name", "Value"], numRows: 5);
        var result = Eval("=Data[#Headers]");
        result.IsArray().Should().BeTrue();
        var values = result.GetValue<CellValue[][]>()!;
        values.Should().HaveCount(1);
        values[0].Select(v => v.GetValue<string>()).Should().Equal("Name", "Value");

        var single = Eval("=Data[[#Headers],[Value]]").GetValue<CellValue[][]>()!;
        single[0].Single().GetValue<string>().Should().Be("Value");
    }

    [Test]
    public void Sheet_Table_Totals_Is_Ref_Error()
    {
        _env.AddSheetTable("Data", Sheet, ["Name", "Value"], numRows: 5);
        ShouldBeError(Eval("=Data[#Totals]"), ErrorType.Ref);
    }

    [Test]
    public void Sheet_Table_Column_Covers_All_Rows()
    {
        _env.AddSheetTable("Data", Sheet, ["Name", "Value"], numRows: 5);
        _env.SetCellValue(0, 1, 1);
        _env.SetCellValue(4, 1, 2);
        Eval("=SUM(Data[Value])").GetValue<double>().Should().Be(3);
        ShouldBeRegion(EvalReference("=Data[Value]"), 0, 4, 1, 1);
        ShouldBeRegion(EvalReference("=Data[#All]"), 0, 4, 0, 1);
        ShouldBeRegion(EvalReference("=Data[[#Headers],[#Data]]"), 0, 4, 0, 1);
    }

    [Test]
    public void Sheet_Table_Of_Whole_Columns_Resolves_To_A_Column_Range()
    {
        _env.AddSheetTable("Data", Sheet, ["Name", "Value"]);
        var reference = EvalReference("=Data[Value]");
        reference.Should().BeOfType<RangeReference>();
        reference.Region.Should().BeOfType<ColumnRegion>();
        reference.Region.Left.Should().Be(1);
        reference.Region.Right.Should().Be(1);
        EvalReference("=Data[#Data]").Region.Should().BeEquivalentTo(new ColumnRegion(0, 1));
        // a row is not a whole column
        ShouldBeRegion(EvalReference("=Data[@]", 7, 9), 7, 7, 0, 1);
    }

    [Test]
    public void Sheet_Table_This_Row_Works_Anywhere_On_The_Sheet()
    {
        _env.AddSheetTable("Data", Sheet, ["Name", "Value"], numRows: 10);
        _env.SetCellValue(3, 1, 42);
        Eval("=[@Value]", 3, 0).GetValue<double>().Should().Be(42);
        Eval("=[@Value]", 3, 20).GetValue<double>().Should().Be(42);
        Eval("=Data[@Value]", 3, 5).GetValue<double>().Should().Be(42);
    }

    [Test]
    public void Unqualified_Reference_On_A_Sheet_Table_Resolves_Anywhere_On_The_Sheet()
    {
        _env.AddSheetTable("Data", Sheet, ["Name", "Value"], numRows: 3);
        _env.SetCellValue(1, 1, 5);
        Eval("=SUM([Value])", 0, 50).GetValue<double>().Should().Be(5);
        var otherSheet = _evaluator.Evaluate(_parser.FromString("=SUM([Value])"),
            caller: new FormulaCallerInfo(0, 0, "Sheet2"));
        ShouldBeError(otherSheet, ErrorType.Ref);
    }

    [Test]
    public void Reference_Takes_The_Sheet_Name_Of_The_Table()
    {
        _env.SetCellValue(0, 0, "Qty");
        _env.AddTable("Other", "Sheet2", new Region(0, 3, 0, 0));
        var reference = EvalReference("=Other[Qty]");
        reference.SheetName.Should().Be("Sheet2");
        reference.ExplicitSheetName.Should().BeTrue();
    }
}
