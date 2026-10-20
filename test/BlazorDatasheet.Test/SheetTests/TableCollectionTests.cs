using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class TableCollectionTests
{
    private Workbook _workbook = null!;
    private Sheet _sheet = null!;
    private List<TableChangedEventArgs> _changes = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _sheet = _workbook.AddSheet(20, 20);
        _changes = new List<TableChangedEventArgs>();
        _workbook.Tables.TableChanged += (_, args) => _changes.Add(args);
    }

    /// <summary>
    /// Sales over A1:C4: headers Qty/Price/Amount and three data rows (Amount 10, 20, 30).
    /// </summary>
    private SheetTable AddSales(bool showTotals = false)
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

        var bottom = showTotals ? 4 : 3;
        return _workbook.Tables.Add("Sales", _sheet.Name, new Region(0, bottom, 0, 2), showTotals)!;
    }

    private IRegion? RegionOf(string name) => _workbook.Tables.Get(name)!.Region;

    private static StructuredReferenceDescriptor Parse(string body, string? tableName = "Sales")
    {
        StructuredReferenceSyntax.TryParseBody(body, tableName, out var descriptor, out var error)
            .Should().BeTrue(error);
        return descriptor!;
    }

    [Test]
    public void Added_Table_Reads_Back_With_Regions_And_Column_Names()
    {
        var table = AddSales();

        table.Name.Should().Be("Sales");
        table.SheetName.Should().Be(_sheet.Name);
        table.IsSheetTable.Should().BeFalse();
        table.IsDeleted.Should().BeFalse();
        table.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        table.HeaderRegion.Should().BeEquivalentTo(new Region(0, 0, 0, 2));
        table.DataRegion.Should().BeEquivalentTo(new Region(1, 3, 0, 2));
        table.TotalsRegion.Should().BeNull();
        table.ColumnNames.Should().Equal("Qty", "Price", "Amount");
        _changes.Should().ContainSingle(c => c.Kind == TableChangeKind.Added && c.Name == "Sales");
    }

    [Test]
    public void Table_With_Totals_Has_Totals_Row()
    {
        var table = AddSales(showTotals: true);

        table.DataRegion.Should().BeEquivalentTo(new Region(1, 3, 0, 2));
        table.TotalsRegion.Should().BeEquivalentTo(new Region(4, 4, 0, 2));
    }

    [Test]
    public void Lookup_Ignores_Case()
    {
        AddSales();

        _workbook.Tables.Get("sALES")!.Name.Should().Be("Sales");
        _workbook.Tables.Contains("SALES").Should().BeTrue();
        _workbook.Tables.Get("sales")!.ColumnNames.Should().Equal("Qty", "Price", "Amount");
        _workbook.Tables.GetTableNames().Should().Equal("Sales");
    }

    [Test]
    public void Add_Is_Undoable()
    {
        AddSales();

        _sheet.Commands.Undo();
        _workbook.Tables.Get("Sales").Should().BeNull();
        _changes.Last().Kind.Should().Be(TableChangeKind.Removed);

        _sheet.Commands.Redo();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));
    }

    [Test]
    public void Remove_And_Undo_Restores_Table()
    {
        AddSales(showTotals: true);

        _workbook.Tables.Remove("sales").Should().BeTrue();
        _workbook.Tables.Get("Sales").Should().BeNull();
        _changes.Last().Kind.Should().Be(TableChangeKind.Removed);

        _sheet.Commands.Undo();
        var table = _workbook.Tables.Get("Sales")!;
        table.Region.Should().BeEquivalentTo(new Region(0, 4, 0, 2));
        table.ShowTotals.Should().BeTrue();

        _sheet.Commands.Redo();
        _workbook.Tables.Get("Sales").Should().BeNull();
        _workbook.Tables.Remove("Sales").Should().BeFalse();
    }

    [Test]
    public void Remove_Then_Undo_Keeps_Table_Shifting_With_Earlier_Undo()
    {
        AddSales();
        _sheet.Rows.InsertAt(0, 2);
        _workbook.Tables.Remove("Sales");

        _sheet.Commands.Undo(); // remove
        _sheet.Commands.Undo(); // insert

        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Remove_Or_Resize_Then_Undo_Then_Undo_Insert_Inside_Restores_Region(bool resize)
    {
        AddSales();
        _sheet.Cells.SetFormula(0, 5, "=SUM(Sales[Amount])");
        _sheet.Rows.InsertAt(2, 1);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 4, 0, 2));

        if (resize)
            _workbook.Tables.Resize("Sales", new Region(0, 8, 0, 2)).Should().BeTrue();
        else
            _workbook.Tables.Remove("Sales").Should().BeTrue();

        _sheet.Commands.Undo(); // remove or resize
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 4, 0, 2));
        _sheet.Commands.Undo(); // insert

        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        _sheet.Cells.SetValue(4, 2, 1000);
        _sheet.Cells.GetValue(0, 5).Should().Be(60m);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Redo_Insert_After_Undoing_Remove_Or_Resize_And_Insert_Grows_Table(bool resize)
    {
        AddSales();
        _sheet.Cells.SetFormula(0, 5, "=SUM(Sales[Amount])");
        _sheet.Rows.InsertAt(2, 1);

        if (resize)
            _workbook.Tables.Resize("Sales", new Region(0, 8, 0, 2)).Should().BeTrue();
        else
            _workbook.Tables.Remove("Sales").Should().BeTrue();

        _sheet.Commands.Undo(); // remove or resize
        _sheet.Commands.Undo(); // insert
        _sheet.Commands.Redo(); // insert

        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 4, 0, 2));
        _sheet.Cells.SetValue(5, 2, 1000);
        _sheet.Cells.GetValue(0, 5).Should().Be(60m);
        _sheet.Cells.SetValue(2, 2, 5);
        _sheet.Cells.GetValue(0, 5).Should().Be(65m);
    }

    [Test]
    public void Rename_And_Undo()
    {
        AddSales();

        _workbook.Tables.Rename("sales", "Orders").Should().BeTrue();
        _workbook.Tables.Get("Sales").Should().BeNull();
        _workbook.Tables.Get("Orders")!.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        _changes.Last().Should().BeEquivalentTo(new { Name = "Orders", Kind = TableChangeKind.Renamed, OldName = "Sales" });

        _sheet.Commands.Undo();
        _workbook.Tables.Get("Orders").Should().BeNull();
        _workbook.Tables.Get("Sales").Should().NotBeNull();
        _changes.Last().Should().BeEquivalentTo(new { Name = "Sales", Kind = TableChangeKind.Renamed, OldName = "Orders" });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Undo_Rename_Rejects_A_Name_Taken_On_Another_Sheet_Or_By_A_Variable(bool variable)
    {
        AddSales();
        _workbook.Tables.Rename("Sales", "Orders").Should().BeTrue();
        if (variable)
            _sheet.FormulaEngine.SetVariable("sales", 10);
        else
        {
            var other = _workbook.AddSheet("Other", 10, 10);
            _workbook.Tables.Add("sales", other.Name, new Region(0, 2, 0, 1)).Should().NotBeNull();
        }
        _changes.Clear();

        _sheet.Commands.Undo().Should().BeFalse();

        _workbook.Tables.Get("Orders")!.SheetName.Should().Be(_sheet.Name);
        _sheet.Tables.Contains("Sales").Should().BeFalse();
        _changes.Should().BeEmpty();
    }

    [Test]
    public void Rename_Can_Change_Case_Only()
    {
        AddSales();

        _workbook.Tables.Rename("Sales", "SALES").Should().BeTrue();
        _workbook.Tables.Get("sales")!.Name.Should().Be("SALES");

        _sheet.Commands.Undo().Should().BeTrue();
        _workbook.Tables.Get("sales")!.Name.Should().Be("Sales");
    }

    [Test]
    public void Rename_To_Existing_Or_Invalid_Name_Fails()
    {
        AddSales();
        _workbook.Tables.Add("Other", _sheet.Name, new Region(10, 12, 0, 1));

        _workbook.Tables.Rename("Sales", "other").Should().BeFalse();
        _workbook.Tables.Rename("Sales", "A1").Should().BeFalse();
        _workbook.Tables.Rename("Missing", "X").Should().BeFalse();
    }

    [Test]
    public void Resize_Rereads_Column_Names_And_Undo_Restores()
    {
        AddSales();
        _sheet.Cells.SetValue(0, 3, "Tax");

        _workbook.Tables.Resize("Sales", new Region(0, 5, 0, 3)).Should().BeTrue();
        var table = _workbook.Tables.Get("Sales")!;
        table.Region.Should().BeEquivalentTo(new Region(0, 5, 0, 3));
        table.ColumnNames.Should().Equal("Qty", "Price", "Amount", "Tax");
        _changes.Last().Kind.Should().Be(TableChangeKind.Resized);

        _sheet.Commands.Undo();
        table = _workbook.Tables.Get("Sales")!;
        table.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        table.ColumnNames.Should().Equal("Qty", "Price", "Amount");
    }

    [Test]
    public void Resize_Fails_When_Invalid()
    {
        AddSales();
        _workbook.Tables.Add("Other", _sheet.Name, new Region(10, 12, 0, 1));

        _workbook.Tables.Resize("Sales", new Region(0, 10, 0, 2)).Should().BeFalse(); // overlaps Other
        _workbook.Tables.Resize("Sales", new Region(0, 0, 0, 2)).Should().BeFalse(); // height 1
        _workbook.Tables.Resize("Sales", new Region(0, 30, 0, 2)).Should().BeFalse(); // off the sheet
        _workbook.Tables.Resize("Sales", new Region(0, 5, 0, 2)).Should().BeTrue(); // may overlap itself
    }

    [Test]
    public void Set_Show_Totals_And_Undo()
    {
        AddSales();

        _workbook.Tables.SetShowTotals("Sales", true).Should().BeTrue();
        _workbook.Tables.Get("Sales")!.TotalsRegion.Should().BeEquivalentTo(new Region(3, 3, 0, 2));
        _changes.Last().Kind.Should().Be(TableChangeKind.ShowTotalsChanged);

        _sheet.Commands.Undo();
        _workbook.Tables.Get("Sales")!.ShowTotals.Should().BeFalse();
    }

    [Test]
    public void Add_Fails_For_Duplicate_Name_Ignoring_Case()
    {
        AddSales();

        _workbook.Tables.Add("SALES", _sheet.Name, new Region(10, 12, 0, 1)).Should().BeNull();
        _workbook.Tables.IsValidTableName("sales", out var reason).Should().BeFalse();
        reason.Should().NotBeNull();
    }

    [Test]
    public void Add_Fails_When_Name_Is_A_Variable()
    {
        _workbook.GetFormulaEngine().SetVariable("x", 1);

        _workbook.Tables.Add("x", _sheet.Name, new Region(0, 2, 0, 1)).Should().BeNull();
        _workbook.Tables.Add("X", _sheet.Name, new Region(0, 2, 0, 1)).Should().BeNull();
    }

    [Test]
    public void Add_Fails_When_Name_Is_A_Function()
    {
        _workbook.Tables.Add("SUM", _sheet.Name, new Region(0, 2, 0, 1)).Should().BeNull();
        _workbook.Tables.IsValidTableName("sum", out _).Should().BeFalse();
    }

    [TestCase("A1")]
    [TestCase("AB12")]
    [TestCase("TRUE")]
    [TestCase("1Table")]
    [TestCase("Sales Data")]
    [TestCase("")]
    public void Add_Fails_For_Invalid_Name(string name)
    {
        _workbook.Tables.IsValidTableName(name, out _).Should().BeFalse();
        _workbook.Tables.Add(name, _sheet.Name, new Region(0, 2, 0, 1)).Should().BeNull();
    }

    [TestCase("Sales")]
    [TestCase("_t")]
    [TestCase("Table1.Q")]
    public void Valid_Names_Are_Accepted(string name)
    {
        _workbook.Tables.IsValidTableName(name, out var reason).Should().BeTrue(reason);
    }

    [Test]
    public void Add_Fails_When_Overlapping_Another_Table()
    {
        AddSales();

        _workbook.Tables.Add("Other", _sheet.Name, new Region(3, 6, 2, 4)).Should().BeNull();
        _workbook.Tables.Add("Other", _sheet.Name, new Region(4, 6, 0, 2)).Should().NotBeNull();
    }

    [Test]
    public void Add_Fails_For_Single_Row_Or_Off_Sheet_Region()
    {
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 0, 0, 2)).Should().BeNull();
        _workbook.Tables.Add("T", _sheet.Name, new Region(18, 20, 0, 2)).Should().BeNull();
        _workbook.Tables.Add("T", "NoSuchSheet", new Region(0, 2, 0, 2)).Should().BeNull();
    }

    [Test]
    public void Sheet_Table_And_Region_Tables_Cannot_Share_A_Sheet()
    {
        AddSales();
        _workbook.Tables.AddSheetTable("Whole", _sheet.Name).Should().BeNull();

        var other = _workbook.AddSheet("Other", 10, 3);
        _workbook.Tables.AddSheetTable("Whole", other.Name).Should().NotBeNull();
        _workbook.Tables.Add("T", other.Name, new Region(0, 2, 0, 1)).Should().BeNull();
        _workbook.Tables.AddSheetTable("Whole2", other.Name).Should().BeNull();
    }

    [Test]
    public void Build_Column_Names_Fills_Empty_And_Deduplicates()
    {
        SheetTable.BuildColumnNames(new[] { "A", null, "", " ", "a", "A", "A2" })
            .Should().Equal("A", "Column2", "Column3", "Column4", "a2", "A3", "A22");
        SheetTable.BuildColumnNames(new[] { "Qty", "Price" }).Should().Equal("Qty", "Price");
        SheetTable.BuildColumnNames(new[] { "Column2", null }).Should().Equal("Column2", "Column22");
    }

    [Test]
    public void Column_Names_Differing_Only_By_Surrounding_Whitespace_Are_Duplicates()
    {
        SheetTable.BuildColumnNames(new[] { "Qty", " Qty ", "qty  " }).Should().Equal("Qty", "Qty2", "qty3");
        SheetTable.BuildColumnNames(new[] { " Qty ", "Qty", "Unit Price" }).Should().Equal(" Qty ", "Qty2", "Unit Price");
        SheetTable.BuildColumnNames(new[] { "Qty", "  ", "\t" }).Should().Equal("Qty", "Column2", "Column3");
    }

    [Test]
    public void Column_Is_Found_Ignoring_Surrounding_Whitespace()
    {
        _sheet.Cells.SetValue(0, 0, " Amount ");
        _sheet.Cells.SetValue(0, 1, "Qty");
        var table = _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 1))!;

        table.ColumnNames.Should().Equal(" Amount ", "Qty");
        table.IndexOfColumn("Amount").Should().Be(0);
        table.IndexOfColumn(" amount").Should().Be(0);
        table.IndexOfColumn(" Qty ").Should().Be(1);
        table.IndexOfColumn("Am ount").Should().Be(-1);
    }

    [Test]
    public void Column_Names_Come_From_Header_Cells()
    {
        _sheet.Cells.SetValue(0, 0, "Name");
        _sheet.Cells.SetValue(0, 2, "name");
        _sheet.Cells.SetValue(0, 3, 2024);

        var table = _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 3))!;

        table.ColumnNames.Should().Equal("Name", "Column2", "name2", "2024");
    }

    [Test]
    public void Sheet_Table_Takes_Column_Names_From_Headings_And_Rows_From_Sheet()
    {
        var sheet = _workbook.AddSheet("Data", 10, 3);
        sheet.Columns.SetHeadings(0, 0, "Qty");
        sheet.Columns.SetHeadings(1, 1, "Price");

        var table = _workbook.Tables.AddSheetTable("Data", sheet.Name)!;

        table.IsSheetTable.Should().BeTrue();
        table.Region.Should().BeNull();
        table.HeaderRegion.Should().BeNull();
        table.TotalsRegion.Should().BeNull();
        table.DataRegion.Should().BeEquivalentTo(new Region(0, 9, 0, 2));
        table.ColumnNames.Should().Equal("Qty", "Price", "Column3");

        sheet.Rows.InsertAt(0, 5);
        _workbook.Tables.Get("Data")!.DataRegion.Should().BeEquivalentTo(new Region(0, 14, 0, 2));
    }

    [Test]
    public void Editing_A_Header_Cell_Renames_The_Column_And_Undo_Reverts()
    {
        AddSales();
        _changes.Clear();

        _sheet.Cells.SetValue(0, 2, "Total");

        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Price", "Total");
        var change = _changes.Should().ContainSingle().Subject;
        change.Kind.Should().Be(TableChangeKind.ColumnsRenamed);
        change.Name.Should().Be("Sales");
        change.ColumnRenames.Should().Equal(("Amount", "Total"));

        _sheet.Commands.Undo();
        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Price", "Amount");
        _changes.Last().ColumnRenames.Should().Equal(("Total", "Amount"));
    }

    [Test]
    public void Editing_A_Data_Cell_Does_Not_Rename_Columns()
    {
        AddSales();
        _changes.Clear();

        _sheet.Cells.SetValue(1, 2, "Total");
        _sheet.Cells.SetValue(0, 5, "Outside");

        _changes.Should().BeEmpty();
    }

    [Test]
    public void Editing_A_Heading_Renames_A_Sheet_Table_Column()
    {
        var sheet = _workbook.AddSheet("Data", 10, 2);
        sheet.Columns.SetHeadings(0, 0, "Qty");
        _workbook.Tables.AddSheetTable("Data", sheet.Name);
        _changes.Clear();

        sheet.Columns.SetHeadings(0, 0, "Quantity");

        _workbook.Tables.Get("Data")!.ColumnNames.Should().Equal("Quantity", "Column2");
        _changes.Should().ContainSingle().Which.ColumnRenames.Should().Equal(("Qty", "Quantity"));

        sheet.Commands.Undo();
        _workbook.Tables.Get("Data")!.ColumnNames.Should().Equal("Qty", "Column2");
    }

    [Test]
    public void Inserting_A_Column_Inside_A_Table_Is_Not_A_Rename()
    {
        AddSales();
        _changes.Clear();

        _sheet.Columns.InsertAt(1);
        var table = _workbook.Tables.Get("Sales")!;
        table.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 3));
        table.ColumnNames.Should().Equal("Qty", "Column2", "Price", "Amount");

        _sheet.Commands.Undo();
        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Price", "Amount");

        // a header edit after the structural change is still seen as a rename
        _sheet.Cells.SetValue(0, 1, "Cost");
        _changes.Should().ContainSingle().Which.ColumnRenames.Should().Equal(("Price", "Cost"));
    }

    [Test]
    public void Insert_Rows_At_Or_Before_Table_Moves_It_And_Undo_Redo()
    {
        AddSales();

        _sheet.Rows.InsertAt(0, 2);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(2, 5, 0, 2));
        _workbook.Tables.Get("Sales")!.ColumnNames.Should().Equal("Qty", "Price", "Amount");

        _sheet.Commands.Undo();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));

        _sheet.Commands.Redo();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(2, 5, 0, 2));
    }

    [Test]
    public void Insert_Rows_Inside_Table_Grows_It()
    {
        AddSales();

        _sheet.Rows.InsertAt(2, 2);

        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 5, 0, 2));
    }

    [Test]
    public void Insert_Rows_Just_After_Table_Leaves_It_Alone()
    {
        AddSales();

        _sheet.Rows.InsertAt(4);

        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));
    }

    [Test]
    public void Insert_Columns_Before_And_Just_After_Table()
    {
        AddSales();

        _sheet.Columns.InsertAt(3);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));

        _sheet.Columns.InsertAt(0);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 1, 3));
    }

    [Test]
    public void Remove_Rows_Inside_Table_Shrinks_It()
    {
        AddSales();

        _sheet.Rows.RemoveAt(1, 2);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 1, 0, 2));

        _sheet.Commands.Undo();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));
    }

    [Test]
    public void Remove_Column_Inside_Table_Shrinks_Column_Names()
    {
        AddSales();

        _sheet.Columns.RemoveAt(1);

        var table = _workbook.Tables.Get("Sales")!;
        table.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 1));
        table.ColumnNames.Should().Equal("Qty", "Amount");
    }

    [Test]
    public void Removing_All_Of_Table_Marks_It_Deleted_And_Undo_Restores()
    {
        AddSales();

        _sheet.Rows.RemoveAt(0, 4);
        var deleted = _workbook.Tables.Get("Sales")!;
        deleted.IsDeleted.Should().BeTrue();
        deleted.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        _workbook.Tables.GetTableNames().Should().BeEmpty();
        _workbook.Tables.Resolve(Parse("Amount"), null, null, null).Error.Should().Be(ErrorType.Ref);

        _sheet.Commands.Undo();
        var restored = _workbook.Tables.Get("Sales")!;
        restored.IsDeleted.Should().BeFalse();
        restored.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));

        _sheet.Commands.Redo();
        _workbook.Tables.Get("Sales")!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public void Removing_All_Columns_Of_Table_Marks_It_Deleted()
    {
        AddSales();

        _sheet.Columns.RemoveAt(0, 3);

        _workbook.Tables.Get("Sales")!.IsDeleted.Should().BeTrue();
    }

    [Test]
    public void Renaming_Sheet_Keeps_Table()
    {
        AddSales();

        _workbook.RenameSheet(_sheet.Name, "Renamed");

        var table = _workbook.Tables.Get("Sales")!;
        table.SheetName.Should().Be("Renamed");
        table.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        _workbook.Tables.Resolve(Parse("Amount"), null, null, null).SheetName.Should().Be("Renamed");
    }

    [Test]
    public void Removing_Sheet_Detaches_Table_And_Adding_It_Back_Reattaches()
    {
        AddSales();
        var sheetName = _sheet.Name;
        _workbook.AddSheet("Other", 5, 5);

        _workbook.RemoveSheet(sheetName);
        var detached = _workbook.Tables.Get("Sales")!;
        detached.SheetName.Should().Be(sheetName);
        detached.Region.Should().BeEquivalentTo(new Region(0, 3, 0, 2));
        _workbook.Tables.GetAll().Should().ContainSingle();
        _workbook.Tables.GetOnSheet(sheetName).Should().ContainSingle();
        _workbook.Tables.Resolve(Parse("Amount"), null, null, null).Error.Should().Be(ErrorType.Ref);
        _workbook.Tables.IsValidTableName("Sales", out _).Should().BeFalse();

        var readded = _workbook.AddSheet(sheetName, 20, 20);
        _workbook.Tables.GetTableAt(sheetName, 1, 1)!.Name.Should().Be("Sales");
        _workbook.Tables.Resolve(Parse("Amount"), null, null, null).Region
            .Should().BeEquivalentTo(new Region(1, 3, 2, 2));

        readded.Rows.InsertAt(0);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(1, 4, 0, 2));
    }

    [Test]
    public void Get_Table_At_Prefers_Region_Table()
    {
        AddSales();

        _workbook.Tables.GetTableAt(_sheet.Name, 2, 1)!.Name.Should().Be("Sales");
        _workbook.Tables.GetTableAt(_sheet.Name, 10, 10).Should().BeNull();
    }

    [Test]
    public void Resolve_Column_Specifiers_And_This_Row()
    {
        AddSales(showTotals: true);
        var tables = _workbook.Tables;

        tables.Resolve(Parse("Amount"), null, null, null).Region.Should().BeEquivalentTo(new Region(1, 3, 2, 2));
        tables.Resolve(Parse("#All"), null, null, null).Region.Should().BeEquivalentTo(new Region(0, 4, 0, 2));
        tables.Resolve(Parse("[#Headers],[Price]"), null, null, null).Region
            .Should().BeEquivalentTo(new Region(0, 0, 1, 1));
        tables.Resolve(Parse("#Totals"), null, null, null).Region.Should().BeEquivalentTo(new Region(4, 4, 0, 2));
        tables.Resolve(Parse("[Amount]:[Qty]"), null, null, null).Region
            .Should().BeEquivalentTo(new Region(1, 3, 0, 2));
        tables.Resolve(Parse("Missing"), null, null, null).Error.Should().Be(ErrorType.Ref);

        tables.Resolve(Parse("@Price", null), 2, 0, _sheet.Name).Region
            .Should().BeEquivalentTo(new Region(2, 2, 1, 1));
        tables.Resolve(Parse("@Price"), 2, 5, _sheet.Name).Region
            .Should().BeEquivalentTo(new Region(2, 2, 1, 1));
        tables.Resolve(Parse("@Price", null), 2, 5, _sheet.Name).Error.Should().Be(ErrorType.Ref);
        tables.Resolve(Parse("@Price"), 0, 5, _sheet.Name).Error.Should().Be(ErrorType.Value);
        tables.Resolve(Parse("@Price"), null, null, null).Error.Should().Be(ErrorType.Value);
        tables.Resolve(Parse("Price", null), 10, 10, _sheet.Name).Error.Should().Be(ErrorType.Ref);
        tables.Resolve(Parse("Price", "Nope"), null, null, null).Error.Should().Be(ErrorType.Ref);
    }

    [Test]
    public void Resolve_Sheet_Table_Headers_As_Values()
    {
        var sheet = _workbook.AddSheet("Data", 10, 2);
        sheet.Columns.SetHeadings(0, 0, "Qty");
        sheet.Columns.SetHeadings(1, 1, "Price");
        _workbook.Tables.AddSheetTable("Data", sheet.Name);

        var headers = _workbook.Tables.Resolve(Parse("#Headers", "Data"), null, null, null);
        headers.Region.Should().BeNull();
        headers.Values!.Single().Select(v => v.Data).Should().Equal("Qty", "Price");

        _workbook.Tables.Resolve(Parse("Price", "Data"), null, null, null).Region
            .Should().BeEquivalentTo(new Region(0, 9, 1, 1));
        _workbook.Tables.Resolve(Parse("#Totals", "Data"), null, null, null).Error.Should().Be(ErrorType.Ref);
        // unqualified refers to the sheet table anywhere on the sheet
        _workbook.Tables.Resolve(Parse("@Qty", null), 4, 1, "Data").Region
            .Should().BeEquivalentTo(new Region(4, 4, 0, 0));
    }

    [Test]
    public void Whole_Column_Or_Row_Regions_Are_Not_Tables()
    {
        _workbook.Tables.Add("Cols", _sheet.Name, new ColumnRegion(0, 1)).Should().BeNull();
        _workbook.Tables.Add("Rows", _sheet.Name, new RowRegion(0, 1)).Should().BeNull();
        AddSales();
        _workbook.Tables.Resize("Sales", new ColumnRegion(0, 2)).Should().BeFalse();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));
    }

    [Test]
    public void Undo_Remove_Does_Not_Restore_A_Table_Whose_Name_Was_Taken_Meanwhile()
    {
        AddSales();
        var other = _workbook.AddSheet("Other", 10, 10);
        _workbook.Tables.Remove("Sales").Should().BeTrue();
        _workbook.Tables.Add("Sales", other.Name, new Region(0, 2, 0, 1)).Should().NotBeNull();
        _changes.Clear();

        _sheet.Commands.Undo().Should().BeFalse();

        _workbook.Tables.GetAll().Where(t => t.Name == "Sales").Should().ContainSingle()
            .Which.SheetName.Should().Be(other.Name);
        _sheet.Tables.Contains("Sales").Should().BeFalse();
        _changes.Should().BeEmpty();
    }

    [Test]
    public void Formula_Using_Structured_Reference_Evaluates()
    {
        AddSales();

        _sheet.Cells.SetFormula(10, 0, "=SUM(Sales[Amount])");
        _sheet.Cells.GetValue(10, 0).Should().Be(60m);

        _sheet.Cells.SetFormula(2, 5, "=Sales[@Qty]*Sales[@Price]");
        _sheet.Cells.GetValue(2, 5).Should().Be(20m);
    }

    [Test]
    public void Unqualified_Formula_Inside_Table_Evaluates()
    {
        AddSales();
        _sheet.Cells.SetValue(0, 3, "Total");
        _workbook.Tables.Resize("Sales", new Region(0, 3, 0, 3));

        _sheet.Cells.SetFormula(2, 3, "=[@Qty]*[@Price]");

        _sheet.Cells.GetValue(2, 3).Should().Be(20m);
    }

    [Test]
    public void Two_Row_Table_With_Totals_Is_Rejected()
    {
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 1, 0, 2), showTotals: true).Should().BeNull();
        _workbook.Tables.CanAdd(_sheet, "T", new Region(0, 1, 0, 2), false, true, out var reason).Should().BeFalse();
        reason.Should().Contain("data row");

        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 1, 0, 2)).Should().NotBeNull();
        _workbook.Tables.Remove("T");
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 2), showTotals: true)!.DataRegion
            .Should().BeEquivalentTo(new Region(1, 1, 0, 2));
    }

    [Test]
    public void Resizing_A_Table_With_Totals_To_Two_Rows_Is_Rejected()
    {
        AddSales(showTotals: true);

        _workbook.Tables.CanResize(_sheet, "Sales", new Region(0, 1, 0, 2), out var reason).Should().BeFalse();
        reason.Should().Contain("data row");
        _workbook.Tables.Resize("Sales", new Region(0, 1, 0, 2)).Should().BeFalse();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 4, 0, 2));

        _workbook.Tables.Resize("Sales", new Region(0, 2, 0, 2)).Should().BeTrue();
        _workbook.Tables.Get("Sales")!.DataRegion.Should().BeEquivalentTo(new Region(1, 1, 0, 2));
    }

    [Test]
    public void Showing_Totals_On_A_Two_Row_Table_Is_Rejected()
    {
        _sheet.Cells.SetValue(0, 0, "Qty");
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 1, 0, 2));
        _changes.Clear();

        _workbook.Tables.CanSetShowTotals(_sheet, "T", true, out var reason).Should().BeFalse();
        reason.Should().Contain("data row");
        _workbook.Tables.SetShowTotals("T", true).Should().BeFalse();

        var table = _workbook.Tables.Get("T")!;
        table.ShowTotals.Should().BeFalse();
        table.DataRegion.Should().BeEquivalentTo(new Region(1, 1, 0, 2));
        _changes.Should().BeEmpty();
        _workbook.Tables.SetShowTotals("T", false).Should().BeTrue();
    }

    [Test]
    public void Loading_A_Two_Row_Table_With_Totals_Skips_It_Unless_Deleted()
    {
        _workbook.Tables.Load("T", _sheet.Name, new Region(0, 1, 0, 2), false, true, false).Should().BeFalse();
        _workbook.Tables.Load("Detached", "Missing", new Region(0, 1, 0, 2), false, true, false).Should().BeFalse();
        _workbook.Tables.Contains("T").Should().BeFalse();
        _workbook.Tables.Contains("Detached").Should().BeFalse();

        _workbook.Tables.Load("Gone", _sheet.Name, new Region(0, 1, 0, 2), false, true, true).Should().BeTrue();
        _workbook.Tables.Get("Gone")!.IsDeleted.Should().BeTrue();
    }

    [Test]
    [SetCulture("en-US")]
    public void Date_Header_Without_A_Format_Is_Named_As_Displayed()
    {
        var date = new System.DateTime(2026, 10, 19);
        var dateTime = new System.DateTime(2026, 1, 2, 13, 5, 0);
        _sheet.Cells.SetValue(0, 0, date);
        _sheet.Cells.SetValue(0, 1, dateTime);
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 1));

        var names = _workbook.Tables.Get("T")!.ColumnNames;
        names.Should().Equal(date.ToString(), dateTime.ToString());
        names[0].Should().NotBe("2026-10-19");
        names.Should().Equal(DisplayedText(0, 0), DisplayedText(0, 1));
    }

    [Test]
    public void Number_Header_Is_Named_As_Rounded_For_Display()
    {
        _sheet.Cells.SetValue(0, 0, 0.1 + 0.2);
        _sheet.Cells.SetValue(1, 0, 4);
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 0));

        _workbook.Tables.Get("T")!.ColumnNames.Should().Equal("0.3");
        DisplayedText(0, 0).Should().Be("0.3");

        _sheet.Cells.SetFormula(5, 5, "=SUM(T[0.3])");
        _sheet.Cells.GetValue(5, 5).Should().Be(4);
    }

    /// <summary>
    /// The text that the datasheet displays for the cell.
    /// </summary>
    private string DisplayedText(int row, int col) =>
        new BlazorDatasheet.Render.VisualCell(row, col, _sheet, CellValueText.DefaultNumberPrecision).FormattedString;

    [Test]
    public void Number_And_Logical_Headers_Are_Named_As_Displayed()
    {
        _sheet.Cells.SetValue(0, 0, 1.5);
        _sheet.Cells.SetValue(0, 1, 1.25);
        _sheet.SetFormat(new Region(0, 0, 1, 1), new CellFormat { NumberFormat = "0.0" });
        _sheet.Cells.SetValue(0, 2, true);
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 2));

        _workbook.Tables.Get("T")!.ColumnNames.Should().Equal("1.5", "1.3", "True");
        _workbook.Tables.Get("T")!.ColumnNames.Should().Equal(DisplayedText(0, 0), DisplayedText(0, 1),
            DisplayedText(0, 2));
    }

    [Test]
    public void Changing_A_Header_Number_Format_Renames_The_Column_And_Undo_Reverts()
    {
        _sheet.Cells.SetValue(0, 0, 1.5);
        _sheet.Cells.SetValue(1, 0, 4);
        _workbook.Tables.Add("T", _sheet.Name, new Region(0, 2, 0, 1));
        _sheet.Cells.SetFormula(5, 5, "=SUM(T[1.5])");
        _changes.Clear();

        _sheet.SetFormat(new Region(0, 0, 0, 0), new CellFormat { NumberFormat = "0.00" });

        _workbook.Tables.Get("T")!.ColumnNames[0].Should().Be("1.50");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(T[1.50])");
        _sheet.Cells.GetValue(5, 5).Should().Be(4);
        _changes.Should().ContainSingle(c => c.Kind == TableChangeKind.ColumnsRenamed);

        _sheet.Commands.Undo();

        _workbook.Tables.Get("T")!.ColumnNames[0].Should().Be("1.5");
        _sheet.Cells.GetFormulaString(5, 5).Should().Be("=SUM(T[1.5])");
        _sheet.Cells.GetValue(5, 5).Should().Be(4);
    }

    [Test]
    public void Detached_Table_Does_Not_Attach_To_A_Sheet_It_Does_Not_Fit()
    {
        var data = _workbook.AddSheet("Data", 30, 10);
        _workbook.Tables.Add("Sales", data.Name, new Region(0, 19, 0, 2)).Should().NotBeNull();
        _workbook.RemoveSheet("Data");

        var small = _workbook.AddSheet("Data", 5, 10);
        small.Tables.Names.Should().BeEmpty();
        _workbook.Tables.GetTableAt("Data", 1, 1).Should().BeNull();
        _workbook.Tables.Get("Sales")!.Region.Should().BeEquivalentTo(new Region(0, 19, 0, 2));
        _workbook.Tables.Resolve(Parse("Qty", "Sales"), null, null, null).Error.Should().Be(ErrorType.Ref);

        // still detached, so it attaches to a later sheet of the name that it fits
        _workbook.RemoveSheet("Data");
        _workbook.AddSheet("Data", 30, 10);
        _workbook.Tables.GetTableAt("Data", 1, 1)!.Name.Should().Be("Sales");
    }

    [Test]
    public void Detached_Table_Does_Not_Attach_Over_A_Table_On_The_Renamed_Sheet()
    {
        var data = _workbook.AddSheet("Data", 30, 10);
        _workbook.Tables.Add("Sales", data.Name, new Region(0, 19, 0, 2)).Should().NotBeNull();
        _workbook.RemoveSheet("Data");

        var other = _workbook.AddSheet("Other", 30, 10);
        _workbook.Tables.Add("Costs", other.Name, new Region(0, 4, 0, 2)).Should().NotBeNull();
        _workbook.RenameSheet("Other", "Data");

        other.Tables.Names.Should().Equal("Costs");
        _workbook.Tables.GetTableAt("Data", 10, 1).Should().BeNull();
        _workbook.Tables.Get("Sales")!.SheetName.Should().Be("Data");
        _workbook.Tables.Contains("Sales").Should().BeTrue();
    }

    [Test]
    public void Detached_Sheet_And_Region_Tables_Do_Not_Attach_Together()
    {
        var data = _workbook.AddSheet("Data", 30, 10);
        _workbook.Tables.AddSheetTable("Whole", data.Name).Should().NotBeNull();
        _workbook.RemoveSheet("Data");

        var other = _workbook.AddSheet("Other", 30, 10);
        _workbook.Tables.Add("Costs", other.Name, new Region(0, 4, 0, 2)).Should().NotBeNull();
        _workbook.RenameSheet("Other", "Data");

        other.Tables.Names.Should().Equal("Costs");
        _workbook.Tables.Get("Whole")!.IsSheetTable.Should().BeTrue();

        // with both detached for the same sheet name, whichever attaches first keeps the other out
        _workbook.RemoveSheet("Data");
        var third = _workbook.AddSheet("Third", 30, 10);
        _workbook.RenameSheet("Third", "Data");
        third.Tables.Names.Should().ContainSingle();
    }

    [Test]
    public void Renaming_A_Detached_Table_Is_Not_Undone_By_A_Sheet()
    {
        var data = _workbook.AddSheet("Data", 30, 10);
        _workbook.Tables.Add("Sales", data.Name, new Region(0, 3, 0, 2)).Should().NotBeNull();
        _workbook.RemoveSheet("Data");

        _workbook.Tables.Rename("Sales", "Orders").Should().BeTrue();
        _sheet.Commands.Undo();

        _workbook.Tables.Contains("Orders").Should().BeTrue();
        _workbook.Tables.Contains("Sales").Should().BeFalse();
        _workbook.AddSheet("Data", 30, 10);
        _workbook.Tables.GetTableAt("Data", 1, 1)!.Name.Should().Be("Orders");
    }

    [Test]
    public void Snapshots_Follow_Changes_Made_Without_Commands()
    {
        AddSales();
        var first = _workbook.Tables.Get("Sales")!;
        _workbook.Tables.Get("Sales").Should().BeSameAs(first);

        _sheet.Rows.InsertAt(0);
        RegionOf("Sales").Should().BeEquivalentTo(new Region(1, 4, 0, 2));
        _sheet.Commands.Undo();
        RegionOf("Sales").Should().BeEquivalentTo(new Region(0, 3, 0, 2));

        _workbook.RenameSheet(_sheet.Name, "Renamed");
        _workbook.Tables.Get("Sales")!.SheetName.Should().Be("Renamed");
    }

    [Test]
    public void Sheet_Table_Snapshot_Follows_The_Sheet_Size()
    {
        _workbook.Tables.AddSheetTable("Whole", _sheet.Name).Should().NotBeNull();
        _workbook.Tables.Get("Whole")!.DataRegion.Should().BeEquivalentTo(new Region(0, 19, 0, 19));

        _sheet.Add(Axis.Row, 5);
        _sheet.Add(Axis.Col, 2);

        var table = _workbook.Tables.Get("Whole")!;
        table.DataRegion.Should().BeEquivalentTo(new Region(0, 24, 0, 21));
        table.ColumnNames.Should().HaveCount(22);
    }
}
