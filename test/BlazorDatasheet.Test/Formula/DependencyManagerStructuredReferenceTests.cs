using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Dependencies;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatashet.Formula.Functions.Math;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class DependencyManagerStructuredReferenceTests
{
    private const string Sheet = "Sheet1";
    private const string Other = "Sheet2";

    private DependencyManager _dm;
    private TestEnvironment _env;

    [SetUp]
    public void Setup_Tests()
    {
        _env = new TestEnvironment();
        _env.RegisterFunction(SumFunction.Descriptor);
        _dm = new DependencyManager();
        _dm.SetStructuredReferenceResolver(_env);
        _dm.AddSheet(Sheet);
        _dm.AddSheet(Other);
    }

    /// <summary>
    /// T over A1:C4 on Sheet1: headers Item/Amt/Double in row 0, data in rows 1-3.
    /// </summary>
    private void AddTable(string name = "T")
    {
        _env.SetCellValue(0, 0, "Item");
        _env.SetCellValue(0, 1, "Amt");
        _env.SetCellValue(0, 2, "Double");
        _env.AddTable(name, Sheet, new Region(0, 3, 0, 2));
    }

    private CellFormula GetFormula(string formulaStr, string sheet = Sheet)
    {
        return new Parser(_env).FromString(formulaStr, new ParsingContext(sheet, false));
    }

    private CellFormula Set(int row, int col, string formula, string sheet = Sheet)
    {
        var parsed = GetFormula(formula, sheet);
        _dm.SetFormula(row, col, sheet, parsed);
        return parsed;
    }

    private static StructuredReference StructuredRef(CellFormula formula) =>
        formula.References.OfType<StructuredReference>().Single();

    /// <summary>
    /// The tracked regions and calculation edges, comparable before and after a restore.
    /// </summary>
    private List<string> Dependencies() =>
        _dm.GetDependencies()
            .Select(d => $"{d.Type}: {d.From} -> {d.To}")
            .Order()
            .ToList();

    private List<string> TrackedRegions() =>
        _dm.GetDependencies()
            .Where(d => d.Type == DependencyType.Region)
            .Select(d => d.To.ToString()!)
            .ToList();

    private DependencyManagerRestoreData InsertRows(int index, int count, string sheet = Sheet)
    {
        _env.InsertRowColIntoTables(index, count, Axis.Row, sheet);
        return _dm.InsertRowColAt(index, count, Axis.Row, sheet);
    }

    private DependencyManagerRestoreData RemoveRows(int index, int count, string sheet = Sheet)
    {
        _env.RemoveRowColFromTables(index, count, Axis.Row, sheet);
        return _dm.RemoveRowColAt(index, count, Axis.Row, sheet);
    }

    [Test]
    public void Set_Formula_Tracks_The_Column_Data()
    {
        AddTable();
        var formula = Set(0, 5, "=SUM(T[Amt])");

        StructuredRef(formula).IsResolved.Should().BeTrue();
        StructuredRef(formula).ResolvedTableName.Should().Be("T");
        for (var row = 1; row <= 3; row++)
            _dm.HasDependents(row, 1, Sheet).Should().BeTrue();
        _dm.HasDependents(0, 1, Sheet).Should().BeFalse("the header isn't part of the column data");
        _dm.HasDependents(4, 1, Sheet).Should().BeFalse();
        _dm.HasDependents(1, 0, Sheet).Should().BeFalse();
        _dm.GetTableDependents("t").Should().ContainSingle();
    }

    [Test]
    public void Clear_Formula_Removes_Tracking()
    {
        AddTable();
        Set(0, 5, "=SUM(T[Amt])");
        _dm.ClearFormula(0, 5, Sheet);

        _dm.GetDependencies().Should().BeEmpty();
        _dm.HasDependents(1, 1, Sheet).Should().BeFalse();
        _dm.GetTableDependents("T").Should().BeEmpty();
    }

    [Test]
    public void Restore_Clear_Formula_Restores_Tracking()
    {
        AddTable();
        Set(0, 5, "=SUM(T[Amt])");
        var before = Dependencies();

        var restoreData = _dm.ClearFormula(0, 5, Sheet);
        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
        _dm.GetTableDependents("T").Should().ContainSingle();
    }

    [Test]
    public void Precedent_Inside_Table_Is_Calculated_First()
    {
        AddTable();
        Set(1, 1, "=10");
        Set(0, 5, "=SUM(T[Amt])");
        // and a precedent set after the formula that depends on it
        Set(2, 1, "=20");

        var order = _dm.GetCalculationOrder().SelectMany(x => x).Select(v => (v.Row, v.Col)).ToList();
        order.IndexOf((1, 1)).Should().BeLessThan(order.IndexOf((0, 5)));
        order.IndexOf((2, 1)).Should().BeLessThan(order.IndexOf((0, 5)));
    }

    [Test]
    public void Named_Formula_Tracks_Table_Column()
    {
        AddTable();
        _dm.SetFormula("x", GetFormula("=SUM(T[Amt])"));

        _dm.HasDependents(2, 1, Sheet).Should().BeTrue();
        _dm.GetTableDependents("T").Should().ContainSingle(v => v.VertexType == VertexType.Named);
    }

    [Test]
    public void Unknown_Table_Is_Unresolved_And_Tracks_Nothing()
    {
        var formula = Set(0, 5, "=SUM(T[Amt])");

        StructuredRef(formula).IsResolved.Should().BeFalse();
        TrackedRegions().Should().BeEmpty();
        _dm.GetTableDependents("T").Should().ContainSingle();
    }

    [Test]
    public void Refresh_Resolves_References_To_A_Table_Added_Later()
    {
        var qualified = Set(0, 0, "=SUM(T[Amt])", Other);
        var unqualified = Set(2, 2, "=[@Amt]*2");
        _dm.GetTableDependents("").Should().ContainSingle();

        AddTable();
        _dm.RefreshStructuredReferences("T").Should().HaveCount(2);

        StructuredRef(qualified).Region.ToString().Should().Be(new Region(1, 3, 1, 1).ToString());
        StructuredRef(unqualified).Region.ToString().Should().Be(new Region(2, 1).ToString());
        _dm.GetTableDependents("T").Should().HaveCount(2);
        _dm.GetTableDependents("").Should().BeEmpty();
        _dm.HasDependents(2, 1, Sheet).Should().BeTrue();
    }

    [TestCase(0, 2, 3, 5, TestName = "Insert before the table")]
    [TestCase(2, 1, 1, 4, TestName = "Insert inside the table")]
    [TestCase(1, 1, 1, 4, TestName = "Insert at the first data row")]
    [TestCase(4, 1, 1, 3, TestName = "Insert immediately after the table")]
    public void Insert_Rows_Tracks_The_New_Data_Region(int index, int count, int dataTop, int dataBottom)
    {
        AddTable();
        var formula = Set(0, 0, "=SUM(T[Amt])", Other);
        var before = Dependencies();

        var restoreData = InsertRows(index, count);

        var expected = new Region(dataTop, dataBottom, 1, 1);
        StructuredRef(formula).Region.ToString().Should().Be(expected.ToString());
        TrackedRegions().Should().Equal(expected.ToString());
        for (var row = dataTop; row <= dataBottom; row++)
            _dm.HasDependents(row, 1, Sheet).Should().BeTrue();
        _dm.HasDependents(dataBottom + 1, 1, Sheet).Should().BeFalse();

        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
        StructuredRef(formula).Region.ToString().Should().Be(new Region(1, 3, 1, 1).ToString());
        formula.ToFormulaString().Should().Be("=SUM(T[Amt])");
    }

    [Test]
    public void Insert_Rows_On_The_Same_Sheet_Shifts_Formula_And_Restores()
    {
        AddTable();
        Set(1, 1, "=10");
        var formula = Set(6, 0, "=SUM(T[Amt])");
        var before = Dependencies();

        var restoreData = InsertRows(2, 2);

        _dm.GetVertex(8, 0, Sheet).Should().NotBeNull();
        StructuredRef(formula).Region.ToString().Should().Be(new Region(1, 5, 1, 1).ToString());
        TrackedRegions().Should().Equal(new Region(1, 5, 1, 1).ToString());

        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
        _dm.GetVertex(6, 0, Sheet).Should().NotBeNull();
        _dm.GetTableDependents("T").Should().ContainSingle();
    }

    [Test]
    public void This_Row_Reference_Follows_Its_Shifted_Vertex()
    {
        AddTable();
        var formula = Set(2, 2, "=[@Amt]*2");
        TrackedRegions().Should().Equal(new Region(2, 1).ToString());
        var before = Dependencies();

        var restoreData = InsertRows(1, 1);

        _dm.GetVertex(3, 2, Sheet).Should().NotBeNull();
        StructuredRef(formula).Region.ToString().Should().Be(new Region(3, 1).ToString());
        TrackedRegions().Should().Equal(new Region(3, 1).ToString());
        _dm.HasDependents(3, 1, Sheet).Should().BeTrue();
        _dm.HasDependents(2, 1, Sheet).Should().BeFalse();

        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
        StructuredRef(formula).Region.ToString().Should().Be(new Region(2, 1).ToString());
    }

    [Test]
    public void Shift_Below_The_Table_And_Its_Formulas_Does_Not_Retrack_Them()
    {
        AddTable();
        var thisRow = StructuredRef(Set(2, 2, "=[@Amt]*2"));
        var column = StructuredRef(Set(6, 0, "=SUM(T[Amt])"));
        var thisRowRegion = thisRow.Region;
        var columnRegion = column.Region;
        var before = Dependencies();

        // retracking resolves each reference to a new region, so the same instances mean they were left alone
        var restoreData = InsertRows(10, 2);
        thisRow.Region.Should().BeSameAs(thisRowRegion);
        column.Region.Should().BeSameAs(columnRegion);
        Dependencies().Should().Equal(before);
        _dm.Restore(restoreData);

        restoreData = RemoveRows(8, 2);
        thisRow.Region.Should().BeSameAs(thisRowRegion);
        column.Region.Should().BeSameAs(columnRegion);
        Dependencies().Should().Equal(before);
        _dm.Restore(restoreData);

        // inserting above moves the table and the formulas, so they are retracked
        restoreData = InsertRows(0, 1);
        thisRow.Region.Should().NotBeSameAs(thisRowRegion);
        column.Region.Should().NotBeSameAs(columnRegion);
        thisRow.Region.ToString().Should().Be(new Region(3, 1).ToString());
        column.Region.ToString().Should().Be(new Region(2, 4, 1, 1).ToString());

        _dm.Restore(restoreData);
        Dependencies().Should().Equal(before);
    }

    [Test]
    public void Shift_Below_The_Table_Retracks_A_Formula_That_Moves()
    {
        AddTable();
        var column = StructuredRef(Set(6, 0, "=SUM(T[Amt])"));
        var columnRegion = column.Region;

        // the table doesn't move, but the formula does
        var restoreData = InsertRows(5, 1);
        _dm.GetVertex(7, 0, Sheet).Should().NotBeNull();
        column.Region.Should().NotBeSameAs(columnRegion);
        column.Region.ToString().Should().Be(new Region(1, 3, 1, 1).ToString());
        TrackedRegions().Should().Equal(new Region(1, 3, 1, 1).ToString());
        _dm.HasDependents(1, 1, Sheet).Should().BeTrue();

        _dm.Restore(restoreData);
        _dm.GetVertex(6, 0, Sheet).Should().NotBeNull();
    }

    [Test]
    public void Remove_Rows_Inside_Table_Shrinks_The_Tracked_Region()
    {
        AddTable();
        var formula = Set(0, 0, "=SUM(T[Amt])", Other);
        var before = Dependencies();

        var restoreData = RemoveRows(2, 1);

        StructuredRef(formula).Region.ToString().Should().Be(new Region(1, 2, 1, 1).ToString());
        TrackedRegions().Should().Equal(new Region(1, 2, 1, 1).ToString());

        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
    }

    [Test]
    public void Remove_Whole_Table_Leaves_Reference_Unresolved_And_Restores()
    {
        AddTable();
        var formula = Set(0, 0, "=SUM(T[Amt])", Other);
        var before = Dependencies();
        var table = _env.GetTable("T")!;

        var restoreData = RemoveRows(0, 4);

        StructuredRef(formula).IsResolved.Should().BeFalse();
        formula.ToFormulaString().Should().Be("=SUM(T[Amt])");
        TrackedRegions().Should().BeEmpty();
        _dm.GetTableDependents("T").Should().ContainSingle();

        _env.SetTable(table);
        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
        StructuredRef(formula).Region.ToString().Should().Be(new Region(1, 3, 1, 1).ToString());
        StructuredRef(formula).ResolvedTableName.Should().Be("T");
    }

    [Test]
    public void Remove_Rows_Containing_Formula_With_This_Row_Reference_Restores()
    {
        AddTable();
        Set(2, 1, "=5");
        Set(2, 2, "=[@Amt]*2");
        var before = Dependencies();

        var restoreData = RemoveRows(2, 1);

        _dm.GetVertex(2, 2, Sheet).Should().BeNull();
        _dm.GetTableDependents("T").Should().BeEmpty();
        TrackedRegions().Should().BeEmpty();

        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
        _dm.GetTableDependents("T").Should().ContainSingle();
    }

    [Test]
    public void Insert_Cells_Retracks_Structured_References()
    {
        AddTable();
        var formula = Set(0, 0, "=SUM(T[Amt])", Other);
        var before = Dependencies();

        // the test environment's tables don't follow cell shifts, so the table is unchanged
        var restoreData = _dm.InsertCellsAt(new Region(1, 1, 5, 5), Axis.Col, Sheet);

        StructuredRef(formula).Region.ToString().Should().Be(new Region(1, 3, 1, 1).ToString());
        TrackedRegions().Should().Equal(new Region(1, 3, 1, 1).ToString());

        _dm.Restore(restoreData);

        Dependencies().Should().Equal(before);
    }

    [Test]
    public void Rename_Table_And_Column_Rewrites_All_Dependents()
    {
        AddTable();
        var cell = Set(0, 0, "=SUM(T[Amt])", Other);
        var named = GetFormula("=SUM(T[Amt])");
        _dm.SetFormula("x", named);
        var unqualified = Set(2, 2, "=[@Amt]*2");

        _env.RenameTable("T", "Sales");
        _dm.RenameTable("T", "Sales");

        cell.ToFormulaString().Should().Be("=SUM(Sales[Amt])");
        named.ToFormulaString().Should().Be("=SUM(Sales[Amt])");
        unqualified.ToFormulaString().Should().Be("=[@Amt]*2");
        _dm.GetTableDependents("Sales").Should().HaveCount(3);
        _dm.GetTableDependents("T").Should().BeEmpty();

        _env.RenameTableColumn("Sales", "Amt", "Amount");
        _dm.RenameTableColumn("Sales", "amt", "Amount");
        _dm.RefreshStructuredReferences("Sales");

        cell.ToFormulaString().Should().Be("=SUM(Sales[Amount])");
        named.ToFormulaString().Should().Be("=SUM(Sales[Amount])");
        unqualified.ToFormulaString().Should().Be("=[@Amount]*2");
        StructuredRef(cell).Region.ToString().Should().Be(new Region(1, 3, 1, 1).ToString());
        StructuredRef(unqualified).Region.ToString().Should().Be(new Region(2, 1).ToString());
    }

    [Test]
    public void Rename_Column_Only_Changes_References_To_That_Table()
    {
        AddTable();
        _env.SetCellValue(10, 0, "Amt");
        _env.AddTable("U", Sheet, new Region(10, 12, 0, 0));
        var t = Set(0, 0, "=SUM(T[Amt])", Other);
        var u = Set(1, 0, "=SUM(U[Amt])", Other);

        _dm.RenameTableColumn("T", "Amt", "Amount");

        t.ToFormulaString().Should().Be("=SUM(T[Amount])");
        u.ToFormulaString().Should().Be("=SUM(U[Amt])");
    }

    [Test]
    public void Formula_Leaving_Table_Does_Not_Leave_A_Stale_Circular_Edge()
    {
        AddTable();
        // the formula in the table refers to the sum, which sums it - a real circular reference
        Set(3, 1, "=F1");
        Set(0, 5, "=SUM(T[Amt])");
        _dm.GetCalculationOrder().Should().Contain(g => g.Count == 2);

        // the table shrinks so the formula is no longer in it
        _env.SetTable(_env.GetTable("T")!.WithRegion(new Region(0, 2, 0, 2)));
        _dm.RefreshStructuredReferences("T");

        _dm.GetCalculationOrder().Should().OnlyContain(g => g.Count == 1);
        var order = _dm.GetCalculationOrder().SelectMany(x => x).Select(v => (v.Row, v.Col)).ToList();
        order.IndexOf((0, 5)).Should().BeLessThan(order.IndexOf((3, 1)));
    }

    [Test]
    public void Edge_Covered_By_Another_Reference_Survives_Untracking()
    {
        AddTable();
        Set(2, 1, "=5");
        Set(0, 5, "=SUM(T[Amt]) + B3");

        _env.SetTable(_env.GetTable("T")!.WithRegion(new Region(0, 1, 0, 2)));
        _dm.RefreshStructuredReferences("T");

        var order = _dm.GetCalculationOrder().SelectMany(x => x).Select(v => (v.Row, v.Col)).ToList();
        order.IndexOf((2, 1)).Should().BeLessThan(order.IndexOf((0, 5)));
        _dm.HasDependents(2, 1, Sheet).Should().BeTrue();
    }

    [Test]
    public void Remove_Sheet_Drops_Table_Dependents_On_It()
    {
        AddTable();
        Set(0, 0, "=SUM(T[Amt])", Other);
        _dm.RemoveSheet(Other);

        _dm.GetTableDependents("T").Should().BeEmpty();
    }

    [Test]
    public void Rename_Sheet_Keeps_Table_Dependents()
    {
        AddTable();
        Set(0, 0, "=SUM(T[Amt])", Other);
        _dm.RenameSheet(Other, "Renamed");

        _dm.GetTableDependents("T").Should().ContainSingle(v => v.SheetName == "Renamed");
        _dm.GetVertex(0, 0, "Renamed").Should().BeSameAs(_dm.GetTableDependents("T").Single());
    }
}
