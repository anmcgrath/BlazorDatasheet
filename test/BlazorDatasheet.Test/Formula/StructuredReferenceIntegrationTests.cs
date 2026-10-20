using System;
using System.Collections.Generic;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class StructuredReferenceIntegrationTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet(20, 10);
    }

    /// <summary>
    /// Sales over A1:C4: headers Qty/Price/Amount and three data rows (Amount 10, 20, 30).
    /// </summary>
    private void AddSales()
    {
        _sheet.Cells.SetValue(0, 0, "Qty");
        _sheet.Cells.SetValue(0, 1, "Price");
        _sheet.Cells.SetValue(0, 2, "Amount");
        for (var i = 1; i <= 3; i++)
        {
            _sheet.Cells.SetValue(i, 0, i);
            _sheet.Cells.SetValue(i, 1, 10);
            _sheet.Cells.SetValue(i, 2, i * 10);
        }

        _workbook.Tables.Add("Sales", _sheet.Name, new Region(0, 3, 0, 2)).Should().NotBeNull();
    }

    private bool HasDependents(int row, int col) =>
        _sheet.FormulaEngine.DependencyManager.HasDependents(row, col, _sheet.Name);

    private static void ShouldBeError(Sheet sheet, int row, int col, ErrorType errorType)
    {
        var value = sheet.Cells.GetCellValue(row, col);
        value.IsError().Should().BeTrue($"expected #{errorType} but was {value.Data}");
        ((FormulaError)value.Data!).ErrorType.Should().Be(errorType);
    }

    /// <summary>
    /// A sheet table "Sales" with column headings Qty/Price and values in the first three rows.
    /// </summary>
    private void AddSheetSales()
    {
        _sheet.Columns.SetHeadings(0, 0, "Qty");
        _sheet.Columns.SetHeadings(1, 1, "Price");
        for (var i = 0; i < 3; i++)
        {
            _sheet.Cells.SetValue(i, 0, i + 1);
            _sheet.Cells.SetValue(i, 1, 10);
        }

        _workbook.Tables.AddSheetTable("Sales", _sheet.Name).Should().NotBeNull();
    }

    [TestCase("=SUM(Sales[ Amount ])", "=SUM(Sales[Amount])", " Amount ")]
    [TestCase("=SUM(Sales[Amount])", "=SUM(Sales[Amount])", " Amount ")]
    [TestCase("=SUM(Sales[ Amount ])", "=SUM(Sales[Amount])", "Amount")]
    [TestCase("=SUM(Sales[[#Data],[ Amount ]])", "=SUM(Sales[[#Data],[Amount]])", " Amount ")]
    [TestCase("=SUM(Sales[[ #Data ],[ Amount ]:[ Amount]])", "=SUM(Sales[[#Data],[Amount]:[Amount]])", "Amount")]
    [TestCase("=Sales[@ Amount ]", "=Sales[@Amount]", " Amount ")]
    [TestCase("=Sales[@[ Amount ]]", "=Sales[@Amount]", "Amount")]
    [TestCase("=[@ Amount ]", "=[@Amount]", " Amount ")]
    public void Column_Whitespace_Around_Names_Is_Ignored_When_Referencing(string formula, string canonical,
        string header)
    {
        _sheet.Cells.SetValue(0, 0, header);
        _sheet.Cells.SetValue(0, 1, "Result");
        _sheet.Cells.SetValue(1, 0, 42);
        _workbook.Tables.Add("Sales", _sheet.Name, new Region(0, 2, 0, 1));

        _sheet.Cells.SetFormula(1, 1, formula);
        _sheet.Cells.GetValue(1, 1).Should().Be(42);
        _sheet.Cells.GetFormulaString(1, 1).Should().Be(canonical);
        _sheet.Cells.SetFormula(1, 1, canonical);
        _sheet.Cells.GetValue(1, 1).Should().Be(42);
        _sheet.Cells.SetValue(1, 0, 84);
        _sheet.Cells.GetValue(1, 1).Should().Be(84);
    }

    [Test]
    public void Changing_Only_The_Whitespace_Around_A_Header_Is_Not_A_Rename()
    {
        _sheet.Cells.SetValue(0, 0, "Amount");
        _sheet.Cells.SetValue(1, 0, 42);
        _workbook.Tables.Add("Sales", _sheet.Name, new Region(0, 2, 0, 1));
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        _sheet.Cells.SetValue(0, 0, " Amount ");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(42);

        _sheet.Cells.SetValue(0, 0, " Total ");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Total])");
        _sheet.Cells.GetValue(5, 5).Should().Be(42);
    }

    [Test]
    public void Sum_Of_Column_Recalculates_When_Data_Changes_But_Does_Not_Depend_On_Header()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        _sheet.Cells.SetValue(2, 2, 100);
        _sheet.Cells.GetValue(5, 5).Should().Be(140);

        HasDependents(2, 2).Should().BeTrue();
        HasDependents(0, 2).Should().BeFalse("the header is not part of the column's data");
        HasDependents(2, 1).Should().BeFalse();
    }

    [Test]
    public void Insert_Row_Inside_Table_Is_Tracked_And_Undo_Restores()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        _sheet.Rows.InsertAt(2);
        _sheet.Cells.GetFormulaString(6, 5).Should().Be("=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(6, 5).Should().Be(60);
        HasDependents(4, 2).Should().BeTrue();

        // the new row is part of the table
        _sheet.Cells.SetValue(2, 2, 5);
        _sheet.Cells.GetValue(6, 5).Should().Be(65);

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(6, 5).Should().Be(60);
        _sheet.Commands.Undo();

        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
        HasDependents(3, 2).Should().BeTrue();
        HasDependents(4, 2).Should().BeFalse();
        _sheet.Cells.SetValue(2, 2, 25);
        _sheet.Cells.GetValue(5, 5).Should().Be(65);
    }

    [Test]
    public void This_Row_Formula_Recalculates_After_Insert_Shifts_It()
    {
        AddSales();
        _sheet.Cells.SetFormula(2, 1, "=[@Amount]*2");
        _sheet.Cells.GetValue(2, 1).Should().Be(40);

        _sheet.Rows.InsertAt(1);
        _sheet.Cells.SetValue(3, 2, 7);
        _sheet.Cells.GetValue(3, 1).Should().Be(14);
    }

    [Test]
    public void This_Row_Formula_Copied_Down_Resolves_To_Each_Row()
    {
        AddSales();
        _sheet.Cells.SetFormula(1, 3, "=Sales[@Qty]*Sales[@Price]");
        _sheet.Cells.GetValue(1, 3).Should().Be(10);

        _sheet.Commands.ExecuteCommand(new CopyRangeCommand(_sheet.Range(1, 3), _sheet.Range(2, 3),
            CopyOptions.DefaultCopyOptions));
        _sheet.Cells.GetFormulaString(2, 3).Should().Be("=Sales[@Qty]*Sales[@Price]");
        _sheet.Cells.GetValue(2, 3).Should().Be(20);

        _sheet.Commands.ExecuteCommand(new AutoFillCommand(new Region(1, 2, 3, 3), new Region(1, 3, 3, 3)));
        _sheet.Cells.GetFormulaString(3, 3).Should().Be("=Sales[@Qty]*Sales[@Price]");
        _sheet.Cells.GetValue(3, 3).Should().Be(30);

        // each clone depends on its own row
        _sheet.Cells.SetValue(2, 0, 5);
        _sheet.Cells.GetValue(1, 3).Should().Be(10);
        _sheet.Cells.GetValue(2, 3).Should().Be(50);
        _sheet.Cells.GetValue(3, 3).Should().Be(30);
    }

    [Test]
    public void Removing_All_Table_Rows_Gives_Ref_And_Keeps_Text_Then_Undo_Restores()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        _sheet.Rows.RemoveAt(0, 4);
        ShouldBeError(_sheet, 1, 5, ErrorType.Ref);
        _sheet.Cells.GetFormulaString(1, 5).Should().Be("=SUM(Sales[Amount])");

        _sheet.Commands.Undo();
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
    }

    [Test]
    public void Rename_Table_Rewrites_Formula_Text_And_Undo_Reverts()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.SetFormula(2, 4, "=Sales[@Amount]");

        _workbook.Tables.Rename("Sales", "Orders").Should().BeTrue();
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Orders[Amount])");
        _sheet.Cells.GetFormulaString(2, 4).Should().Be("=Orders[@Amount]");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
        _sheet.Cells.GetValue(2, 4).Should().Be(20);

        _sheet.Commands.Undo(); // the value
        _sheet.Commands.Undo(); // the rename
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        _sheet.Commands.Redo();
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Orders[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
    }

    [Test]
    public void Editing_Header_Cell_Renames_Column_In_Formulas_And_Undo_Reverts()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.SetFormula(2, 4, "=Sales[@Amount]*2");

        _sheet.Cells.SetValue(0, 2, "Total");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Total])");
        _sheet.Cells.GetFormulaString(2, 4).Should().Be("=Sales[@Total]*2");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);

        _sheet.Commands.Undo(); // the value
        _sheet.Commands.Undo(); // the header edit
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
        _sheet.Cells.GetFormulaString(2, 4).Should().Be("=Sales[@Amount]*2");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.GetValue(2, 4).Should().Be(40);
    }

    [Test]
    public void Swapping_Header_Names_Swaps_Them_In_Formulas()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Qty])+SUM(Sales[Price])*100");
        _sheet.Cells.GetValue(5, 5).Should().Be(3006);

        _sheet.Cells.SetValues(0, 0, new object[][] { new object[] { "Price", "Qty" } });
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Price])+SUM(Sales[Qty])*100");
        _sheet.Cells.GetValue(5, 5).Should().Be(3006);
    }

    [Test]
    public void Header_Cell_Holding_A_Formula_Renames_The_Column_After_Calculating()
    {
        AddSales();
        _sheet.Cells.SetValue(10, 0, "Amount");
        _sheet.Cells.SetFormula(0, 2, "=A11");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        // calculating the header formula renames the column, part way through the calculation
        _sheet.Cells.SetValue(10, 0, "Total");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Total])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
    }

    [Test]
    public void Table_Rename_Asked_For_By_A_Calculation_That_Throws_Is_Still_Applied()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();
        engine.SetVariable("x", 1);
        _sheet.Cells.SetFormula(6, 6, "=x");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        // a calculation outside a batch raises its cell changes before it has finished, so the rename is
        // deferred - and then the calculation throws
        var thrown = false;
        _sheet.Cells.CellsChanged += (_, _) =>
        {
            if (thrown || !_sheet.FormulaEngine.IsCalculating)
                return;
            thrown = true;
            _workbook.Tables.Rename("Sales", "Orders");
            throw new InvalidOperationException("handler failed");
        };

        var act = () => engine.SetVariable("x", 2);
        act.Should().Throw<InvalidOperationException>().WithMessage("handler failed");
        thrown.Should().BeTrue();

        // applied straight away, not only when some later calculation runs
        _workbook.Tables.Get("Orders").Should().NotBeNull();
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Orders[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
    }

    [Test]
    public void Table_Work_Asked_For_By_Every_Calculation_Throws_Instead_Of_Being_Dropped()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();
        engine.SetVariable("x", 1);
        _sheet.Cells.SetFormula(6, 6, "=x");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        // each rename recalculates the SUM, whose change renames the table again - forever
        _sheet.Cells.CellsChanged += (_, _) =>
        {
            if (!_sheet.FormulaEngine.IsCalculating)
                return;
            var name = _workbook.Tables.Get("Sales") != null ? "Sales" : "Orders";
            _workbook.Tables.Rename(name, name == "Sales" ? "Orders" : "Sales");
        };

        var act = () => engine.SetVariable("x", 2);
        act.Should().Throw<InvalidOperationException>().WithMessage("*100 levels deep*");
    }

    [Test]
    public void Header_Formula_Recalculated_Outside_A_Batch_Renames_The_Column_After_Calculating()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();
        engine.SetVariable("heading", "Amount");
        _sheet.Cells.SetFormula(0, 2, "=heading");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        // the header's change is raised while the engine is still calculating, so the rename is deferred
        engine.SetVariable("heading", "Total");
        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Price", "Total");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Total])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
    }

    [Test]
    public void Duplicate_Header_Names_Are_Numbered()
    {
        AddSales();
        _sheet.Cells.SetValue(0, 1, "Qty");
        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Qty2", "Amount");

        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Qty2])");
        _sheet.Cells.GetValue(5, 5).Should().Be(30);
    }

    [Test]
    public void Formula_Written_Before_Its_Table_Resolves_When_Table_Is_Added()
    {
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        ShouldBeError(_sheet, 5, 5, ErrorType.Ref);

        AddSales();
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
    }

    [Test]
    public void Unqualified_Reference_Written_Before_Its_Table_Resolves_When_Table_Is_Added()
    {
        _sheet.Cells.SetFormula(2, 4, "=[@Amount]");
        ShouldBeError(_sheet, 2, 4, ErrorType.Ref);

        _sheet.Cells.SetValue(0, 2, "Amount");
        _sheet.Cells.SetValue(2, 2, 7);
        _workbook.Tables.Add("Sales", _sheet.Name, new Region(0, 3, 0, 4)).Should().NotBeNull();
        _sheet.Cells.GetValue(2, 4).Should().Be(7);
    }

    [Test]
    public void Removing_Table_Gives_Ref_And_Undo_Restores()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        _workbook.Tables.Remove("Sales").Should().BeTrue();
        ShouldBeError(_sheet, 5, 5, ErrorType.Ref);
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Amount])");
        HasDependents(1, 2).Should().BeFalse();

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        HasDependents(1, 2).Should().BeTrue();
    }

    [Test]
    public void Resize_Grows_The_Column_And_Undo_Shrinks_It()
    {
        AddSales();
        _sheet.Cells.SetValue(4, 2, 40);
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        _workbook.Tables.Resize("Sales", new Region(0, 4, 0, 2)).Should().BeTrue();
        _sheet.Cells.GetValue(5, 5).Should().Be(100);
        _sheet.Cells.SetValue(4, 2, 50);
        _sheet.Cells.GetValue(5, 5).Should().Be(110);

        _sheet.Commands.Undo(); // the value
        _sheet.Commands.Undo(); // the resize
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        HasDependents(4, 2).Should().BeFalse();
    }

    [Test]
    public void Show_Totals_Excludes_Last_Row_From_Data()
    {
        AddSales();
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.SetFormula(5, 6, "=Sales[[#Totals],[Amount]]");
        ShouldBeError(_sheet, 5, 6, ErrorType.Ref);

        _workbook.Tables.SetShowTotals("Sales", true).Should().BeTrue();
        _sheet.Cells.GetValue(5, 5).Should().Be(30);
        _sheet.Cells.GetValue(5, 6).Should().Be(30);

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        ShouldBeError(_sheet, 5, 6, ErrorType.Ref);
    }

    [Test]
    public void Sheet_Table_Headers_Come_From_Column_Headings_And_Follow_Changes()
    {
        AddSheetSales();
        // the headings spill down column F from row 6
        _sheet.Cells.SetFormula(5, 5, "=TRANSPOSE(Sales[#Headers])");
        _sheet.Cells.SetFormula(5, 7, "=SUM(Sales[Price])");
        _sheet.Cells.GetValue(6, 5).Should().Be("Price");
        _sheet.Cells.GetValue(7, 5).Should().Be("Column3");
        _sheet.Cells.GetValue(5, 7).Should().Be(30);

        _sheet.Columns.SetHeadings(1, 1, "Cost");
        _sheet.Cells.GetValue(6, 5).Should().Be("Cost");
        _sheet.Cells.GetFormulaString(5, 7).Should().Be("=SUM(Sales[Cost])");
        _sheet.Cells.GetValue(5, 7).Should().Be(30);

        _sheet.Commands.Undo();
        _sheet.Cells.GetValue(6, 5).Should().Be("Price");
        _sheet.Cells.GetFormulaString(5, 7).Should().Be("=SUM(Sales[Price])");
    }

    [Test]
    public void Sheet_Table_This_Row_Works_Anywhere_And_Insert_Extends_Columns()
    {
        AddSheetSales();
        _sheet.Cells.SetFormula(1, 5, "=[@Qty]*[@Price]");
        _sheet.Cells.GetValue(1, 5).Should().Be(20);

        _sheet.Cells.SetFormula(15, 6, "=SUM(Sales[Qty])");
        _sheet.Cells.GetValue(15, 6).Should().Be(6);

        _sheet.Rows.InsertAt(0);
        _sheet.Cells.SetValue(0, 0, 100);
        _sheet.Cells.GetValue(16, 6).Should().Be(106);
        _sheet.Cells.SetValue(2, 0, 3);
        _sheet.Cells.GetValue(2, 5).Should().Be(30);
    }

    [Test]
    public void Named_Range_Can_Be_A_Structured_Reference()
    {
        AddSales();
        _workbook.NamedRanges.Set("x", "Sales[Amount]").Should().BeTrue();
        _workbook.NamedRanges.IsNamedRange("x").Should().BeTrue();

        _sheet.Cells.SetFormula(5, 5, "=SUM(x)");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
        _sheet.Cells.SetValue(1, 2, 20);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
    }

    [Test]
    public void Variable_Formula_With_Structured_Reference_Needs_No_Sheet_Name()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();
        engine.SetVariable("v", "=SUM(Sales[Amount])");
        engine.TryGetVariable("v", out var value).Should().BeTrue();
        value.Data.Should().Be(60);

        _sheet.Cells.SetValue(1, 2, 20);
        engine.TryGetVariable("v", out value).Should().BeTrue();
        value.Data.Should().Be(70);
    }

    [Test]
    public void Conditional_Format_Style_Evaluation_Uses_The_Caller_Row()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();
        var formula = engine.ParseFormula("=[@Amount]>10", _sheet.Name);

        engine.EvaluateFormulaAt(formula, new CellPosition(1, 0), 1, 0, _sheet.Name).Data.Should().Be(false);
        engine.EvaluateFormulaAt(formula, new CellPosition(1, 0), 2, 0, _sheet.Name).Data.Should().Be(true);
        engine.EvaluateFormulaAt(formula, new CellPosition(1, 0), 3, 1, _sheet.Name).Data.Should().Be(true);
    }

    [Test]
    public void Renaming_The_Sheet_Keeps_Formulas_Working()
    {
        AddSales();
        var other = _workbook.AddSheet("Other", 10, 10);
        other.Cells.SetFormula(0, 0, "=SUM(Sales[Amount])");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        _workbook.RenameSheet(_sheet.Name, "Renamed");
        _workbook.Tables.Get("Sales")!.SheetName.Should().Be("Renamed");
        _sheet.Cells.SetValue(1, 2, 20);
        other.Cells.GetValue(0, 0).Should().Be(70);
        _sheet.Cells.GetValue(5, 5).Should().Be(70);
    }

    [Test]
    public void Removing_The_Sheet_Gives_Ref_And_Re_Adding_Restores()
    {
        AddSales();
        var sheetName = _sheet.Name;
        var other = _workbook.AddSheet("Other", 10, 10);
        other.Cells.SetFormula(0, 0, "=SUM(Sales[Amount])");
        other.Cells.GetValue(0, 0).Should().Be(60);

        _workbook.RemoveSheet(sheetName);
        ShouldBeError(other, 0, 0, ErrorType.Ref);
        other.Cells.GetFormulaString(0, 0).Should().Be("=SUM(Sales[Amount])");

        var readded = _workbook.AddSheet(sheetName, 20, 10);
        readded.Cells.SetValue(1, 2, 5);
        other.Cells.GetValue(0, 0).Should().Be(5);
    }

    [Test]
    public void Table_Changed_Fires_For_Each_Kind()
    {
        var kinds = new List<TableChangeKind>();
        _workbook.Tables.TableChanged += (_, args) => kinds.Add(args.Kind);

        AddSales();
        _sheet.Cells.SetValue(0, 2, "Total");
        _workbook.Tables.Rename("Sales", "Orders");
        _workbook.Tables.Resize("Orders", new Region(0, 4, 0, 2));
        _workbook.Tables.SetShowTotals("Orders", true);
        _workbook.Tables.Remove("Orders");

        kinds.Should().Equal(TableChangeKind.Added, TableChangeKind.ColumnsRenamed, TableChangeKind.Renamed,
            TableChangeKind.Resized, TableChangeKind.ShowTotalsChanged, TableChangeKind.Removed);
    }

    [Test]
    public void Variables_And_Tables_Cannot_Share_Names()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();

        var setVariable = () => engine.SetVariable("sales", 1);
        setVariable.Should().Throw<Exception>();
        _workbook.NamedRanges.Set("SALES", "A1:A2").Should().BeFalse();
        engine.VariableExists("sales").Should().BeFalse();

        engine.SetVariable("v", 1);
        _workbook.Tables.Add("v", _sheet.Name, new Region(10, 12, 0, 1)).Should().BeNull();
    }

    [Test]
    public void Volatile_Header_Formula_Does_Not_Recalculate_Without_End()
    {
        AddSales();
        _sheet.Cells.SetFormula(0, 1, "=RAND()");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");

        var setValue = () => _sheet.Cells.SetValue(1, 2, 100);
        setValue.Should().NotThrow();
        _sheet.Cells.GetValue(5, 5).Should().Be(150);
    }

    [Test]
    public void Header_Formula_Reading_Another_Cell_Renames_The_Column()
    {
        AddSales();
        _sheet.Cells.SetValue(0, 4, "Amount");
        _sheet.Cells.SetFormula(0, 2, "=IF(TRUE,E1,\"\")");
        _sheet.Cells.SetFormula(5, 5, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);

        _sheet.Cells.SetValue(0, 4, "Total");
        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Price", "Total");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(Sales[Total])");
        _sheet.Cells.GetValue(5, 5).Should().Be(60);
    }

    [Test]
    public void Renaming_A_Detached_Table_Renames_It_In_Formulas()
    {
        var sheet2 = _workbook.AddSheet("Sheet2", 20, 10);
        sheet2.Cells.SetValues(0, 0, new object[][] { new object[] { "Amount" }, new object[] { 5 } });
        _workbook.Tables.Add("Sales", sheet2.Name, new Region(0, 1, 0, 0)).Should().NotBeNull();
        _sheet.Cells.SetFormula(0, 0, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(0, 0).Should().Be(5);

        _workbook.RemoveSheet("Sheet2");
        _workbook.Tables.Rename("Sales", "Orders").Should().BeTrue();
        _sheet.Cells.GetFormulaString(0, 0).Should().Be("=SUM(Orders[Amount])");
        ShouldBeError(_sheet, 0, 0, ErrorType.Ref);

        var readded = _workbook.AddSheet("Sheet2", 20, 10);
        readded.Cells.SetValues(0, 0, new object[][] { new object[] { "Amount" }, new object[] { 7 } });
        _sheet.Cells.GetValue(0, 0).Should().Be(7);
    }

    [Test]
    public void Deleted_And_Detached_Tables_Keep_Their_Names_From_Variables()
    {
        AddSales();
        var engine = _workbook.GetFormulaEngine();
        _sheet.Rows.RemoveAt(0, 4);
        _workbook.Tables.Get("Sales")!.IsDeleted.Should().BeTrue();
        var setDeleted = () => engine.SetVariable("Sales", 1);
        setDeleted.Should().Throw<Exception>();

        var sheet2 = _workbook.AddSheet("Sheet2", 20, 10);
        _workbook.Tables.Add("Orders", sheet2.Name, new Region(0, 2, 0, 1)).Should().NotBeNull();
        _workbook.RemoveSheet("Sheet2");
        var setDetached = () => engine.SetVariable("orders", 1);
        setDetached.Should().Throw<Exception>();
        _workbook.NamedRanges.Set("Orders", "A1:A2").Should().BeFalse();
    }

    [Test]
    public void Changing_A_Sheet_Table_Heading_Calculates_Once()
    {
        AddSheetSales();
        _sheet.Cells.SetFormula(5, 5, "=TRANSPOSE(Sales[#Headers])");
        _sheet.Cells.SetFormula(5, 6, "=SUM(Sales[Price])");
        var completed = 0;
        _workbook.GetFormulaEngine().CalculationCompleted += (_, _) => completed++;

        _sheet.Columns.SetHeadings(1, 1, "Cost");

        completed.Should().Be(1);
        _sheet.Cells.GetValue(6, 5).Should().Be("Cost");
        _sheet.Cells.GetFormulaString(5, 6).Should().Be("=SUM(Sales[Cost])");
    }

    [Test]
    public void Removing_A_Detached_Table_Refreshes_The_Formulas_That_Refer_To_It()
    {
        var sheet2 = _workbook.AddSheet("Sheet2", 20, 10);
        sheet2.Cells.SetValues(0, 0, new object[][] { new object[] { "Amount" }, new object[] { 5 } });
        _workbook.Tables.Add("Sales", sheet2.Name, new Region(0, 1, 0, 0)).Should().NotBeNull();
        _sheet.Cells.SetFormula(0, 0, "=SUM(Sales[Amount])");
        _workbook.RemoveSheet("Sheet2");
        ShouldBeError(_sheet, 0, 0, ErrorType.Ref);

        var evaluated = 0;
        _workbook.GetFormulaEngine().CalculationCompleted += (_, args) => evaluated += args.FormulaCount;
        _workbook.Tables.Remove("Sales").Should().BeTrue();

        evaluated.Should().Be(1);
        _workbook.Tables.Contains("Sales").Should().BeFalse();
        ShouldBeError(_sheet, 0, 0, ErrorType.Ref);

        // a new table of the name is picked up
        _sheet.Cells.SetValues(5, 0, new object[][] { new object[] { "Amount" }, new object[] { 9 } });
        _workbook.Tables.Add("Sales", _sheet.Name, new Region(5, 6, 0, 0)).Should().NotBeNull();
        _sheet.Cells.GetValue(0, 0).Should().Be(9);
    }
}
