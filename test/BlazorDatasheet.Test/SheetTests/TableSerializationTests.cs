using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Serialization.Json;
using BlazorDatasheet.DataStructures.Geometry;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class TableSerializationTests
{
    private Workbook _workbook = null!;
    private Sheet _data = null!;

    [SetUp]
    public void Setup()
    {
        _workbook = new Workbook();
        _data = _workbook.AddSheet("Data", 20, 10);
    }

    /// <summary>
    /// Sales over A1:C5 on Data: headers Qty/Price/Amount, three data rows (Amount 10, 20, 30) and a totals row.
    /// F2:F4 hold Sales[@Qty]*Sales[@Price] and F10 holds SUM(Sales[Amount]).
    /// </summary>
    private void AddSales()
    {
        _data.Cells.SetValue(0, 0, "Qty");
        _data.Cells.SetValue(0, 1, "Price");
        _data.Cells.SetValue(0, 2, "Amount");
        for (var i = 1; i <= 3; i++)
        {
            _data.Cells.SetValue(i, 0, i);
            _data.Cells.SetValue(i, 1, 10);
            _data.Cells.SetValue(i, 2, i * 10);
        }

        _workbook.Tables.Add("Sales", _data.Name, new Region(0, 4, 0, 2), showTotals: true).Should().NotBeNull();

        for (var i = 1; i <= 3; i++)
            _data.Cells.SetFormula(i, 5, "=Sales[@Qty]*Sales[@Price]");
        _data.Cells.SetFormula(9, 5, "=SUM(Sales[Amount])");
    }

    private static Workbook RoundTrip(Workbook workbook, bool calculate = true) =>
        new SheetJsonDeserializer().Deserialize(new SheetJsonSerializer().Serialize(workbook), calculate: calculate);

    [Test]
    public void Tables_Round_Trip_Including_Deleted_And_Detached_Ones()
    {
        AddSales();

        var list = _workbook.AddSheet("List", 10, 5);
        list.Columns.SetHeadings(0, 0, "Item");
        list.Columns.SetHeadings(1, 1, "Cost");
        _workbook.Tables.AddSheetTable("Items", list.Name).Should().NotBeNull();

        _data.Cells.SetValue(12, 0, "X");
        _workbook.Tables.Add("Old", _data.Name, new Region(12, 13, 0, 1)).Should().NotBeNull();
        _data.Rows.RemoveAt(12, 2);

        var gone = _workbook.AddSheet("Gone", 10, 5);
        gone.Cells.SetValue(0, 0, "A");
        _workbook.Tables.Add("Lost", gone.Name, new Region(0, 2, 0, 1)).Should().NotBeNull();
        _workbook.RemoveSheet(gone.Name);

        var restored = RoundTrip(_workbook);

        var sales = restored.Tables.Get("Sales")!;
        sales.SheetName.Should().Be("Data");
        sales.Region.Should().BeEquivalentTo(new Region(0, 4, 0, 2));
        sales.ShowTotals.Should().BeTrue();
        sales.IsDeleted.Should().BeFalse();
        sales.ColumnNames.Should().Equal("Qty", "Price", "Amount");

        var items = restored.Tables.Get("Items")!;
        items.IsSheetTable.Should().BeTrue();
        items.SheetName.Should().Be("List");
        items.ColumnNames.Take(2).Should().Equal("Item", "Cost");

        var old = restored.Tables.Get("Old")!;
        old.IsDeleted.Should().BeTrue();
        old.Region.Should().BeEquivalentTo(new Region(12, 13, 0, 1));

        var lost = restored.Tables.Get("Lost")!;
        lost.SheetName.Should().Be("Gone");
        lost.Region.Should().BeEquivalentTo(new Region(0, 2, 0, 1));
        restored.Tables.GetTableNames().Should().BeEquivalentTo("Sales", "Items");

        // the detached table reads its column names when its sheet comes back
        var regone = restored.AddSheet("Gone", 10, 5);
        regone.Cells.SetValue(0, 0, "First");
        regone.Cells.SetValue(0, 1, "Second");
        restored.Tables.Get("Lost")!.ColumnNames.Should().Equal("First", "Second");
    }

    [Test]
    public void Structured_References_Evaluate_After_Load_And_Recalculate_On_Edit()
    {
        AddSales();

        var restored = RoundTrip(_workbook);
        var data = restored.GetSheet("Data")!;

        data.Cells.GetValue(9, 5).Should().Be(60);
        data.Cells.GetValue(2, 5).Should().Be(20);

        data.Cells.SetValue(2, 2, 100);
        data.Cells.GetValue(9, 5).Should().Be(140);

        data.Cells.SetValue(2, 1, 7);
        data.Cells.GetValue(2, 5).Should().Be(14);
    }

    [Test]
    public void Loading_Without_Calculation_Then_Calculating_Gives_Correct_Values()
    {
        AddSales();

        var restored = RoundTrip(_workbook, calculate: false);
        restored.GetFormulaEngine().CalculateSheet(true);
        var data = restored.GetSheet("Data")!;

        data.Cells.GetValue(9, 5).Should().Be(60);
        data.Cells.GetValue(1, 5).Should().Be(10);
        data.Cells.GetValue(3, 5).Should().Be(30);

        data.Cells.SetValue(3, 2, 0);
        data.Cells.GetValue(9, 5).Should().Be(30);
    }

    [Test]
    public void Variable_Referring_To_A_Table_Round_Trips()
    {
        AddSales();
        _workbook.GetFormulaEngine().SetVariable("total", "=SUM(Sales[Amount])");

        var restored = RoundTrip(_workbook);
        var engine = restored.GetFormulaEngine();

        engine.TryGetVariable("total", out var value).Should().BeTrue();
        value.Data.Should().Be(60);

        restored.GetSheet("Data")!.Cells.SetValue(1, 2, 20);
        engine.TryGetVariable("total", out value).Should().BeTrue();
        value.Data.Should().Be(70);
    }

    [Test]
    public void Workbook_Without_Tables_Serializes_Without_The_Section()
    {
        var json = new SheetJsonSerializer().Serialize(_workbook);

        json.Should().NotContain("\"Tables\"");
        new SheetJsonDeserializer().Deserialize(json).Tables.GetAll().Should().BeEmpty();
    }

    [Test]
    public void Json_Without_A_Tables_Property_Loads()
    {
        _data.Cells.SetValue(0, 0, 5);
        var json = new SheetJsonSerializer().Serialize(_workbook);
        json.Should().NotContain("\"Tables\"");

        var withNull = json.Insert(json.LastIndexOf('}'), ",\"Tables\":null");

        new SheetJsonDeserializer().Deserialize(json).GetSheet("Data")!.Cells.GetValue(0, 0).Should().Be(5);
        new SheetJsonDeserializer().Deserialize(withNull).Tables.GetAll().Should().BeEmpty();
    }
}
