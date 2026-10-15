using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class BandLimitedReferenceTests
{
    // B2:B3 - a band of rows 2:3, one column wide at column B
    private static readonly IRegion BandAtB = new Region(1, 2, 1, 1);

    // B2:C2 - a band of columns B:C, one row high at row 2
    private static readonly IRegion BandAtRow2 = new Region(1, 1, 1, 2);

    private static CellFormula Parse(string formula, string sheet = "Sheet1")
    {
        return new Parser(new TestEnvironment()).FromString(formula, new ParsingContext(sheet, false));
    }

    private static string InsertCols(string formula, IRegion band)
    {
        var f = Parse(formula);
        f.InsertRowColIntoReferences(band.Left, band.Width, Axis.Col, "Sheet1", band);
        return f.ToFormulaString();
    }

    private static string RemoveCols(string formula, IRegion band)
    {
        var f = Parse(formula);
        f.RemoveRowColFromReferences(band.Left, band.Width, Axis.Col, "Sheet1", band);
        return f.ToFormulaString();
    }

    private static string InsertRows(string formula, IRegion band)
    {
        var f = Parse(formula);
        f.InsertRowColIntoReferences(band.Top, band.Height, Axis.Row, "Sheet1", band);
        return f.ToFormulaString();
    }

    private static string RemoveRows(string formula, IRegion band)
    {
        var f = Parse(formula);
        f.RemoveRowColFromReferences(band.Top, band.Height, Axis.Row, "Sheet1", band);
        return f.ToFormulaString();
    }

    [TestCase("=B2", "=C2")]
    [TestCase("=B3", "=C3")]
    [TestCase("=sum(B2:C2)", "=sum(C2:D2)")]
    [TestCase("=sum(A2:C2)", "=sum(A2:D2)")]
    [TestCase("=sum(B2:C3)", "=sum(C2:D3)")]
    [TestCase("=sum(A1:C5)", "=sum(A1:C5)")]
    [TestCase("=sum(A2:C4)", "=sum(A2:C4)")]
    [TestCase("=sum(B:B)", "=sum(B:B)")]
    [TestCase("=sum(2:2)", "=sum(2:2)")]
    [TestCase("=$B$2", "=$C$2")]
    [TestCase("=A2", "=A2")]
    [TestCase("=B1", "=B1")]
    [TestCase("=B4", "=B4")]
    [TestCase("=Sheet2!B2", "=Sheet2!B2")]
    public void Insert_Cells_Shift_Right_Adjusts_Only_References_Within_Band(string formula, string expected)
    {
        InsertCols(formula, BandAtB).Should().Be(expected);
    }

    [TestCase("=B2", "=#REF!")]
    [TestCase("=C2", "=B2")]
    [TestCase("=sum(A2:C2)", "=sum(A2:B2)")]
    [TestCase("=sum(B2:D3)", "=sum(B2:C3)")]
    [TestCase("=sum(A1:C5)", "=sum(A1:C5)")]
    [TestCase("=sum(B:B)", "=sum(B:B)")]
    [TestCase("=sum(2:2)", "=sum(2:2)")]
    [TestCase("=A2", "=A2")]
    [TestCase("=C1", "=C1")]
    [TestCase("=Sheet2!C2", "=Sheet2!C2")]
    public void Remove_Cells_Shift_Left_Adjusts_Only_References_Within_Band(string formula, string expected)
    {
        RemoveCols(formula, BandAtB).Should().Be(expected);
    }

    [TestCase("=B2", "=B3")]
    [TestCase("=C2", "=C3")]
    [TestCase("=sum(B2:C4)", "=sum(B3:C5)")]
    [TestCase("=sum(B1:B3)", "=sum(B1:B4)")]
    [TestCase("=sum(A1:C5)", "=sum(A1:C5)")]
    [TestCase("=sum(B:B)", "=sum(B:B)")]
    [TestCase("=sum(2:2)", "=sum(2:2)")]
    [TestCase("=D2", "=D2")]
    public void Insert_Cells_Shift_Down_Adjusts_Only_References_Within_Band(string formula, string expected)
    {
        InsertRows(formula, BandAtRow2).Should().Be(expected);
    }

    [TestCase("=B2", "=#REF!")]
    [TestCase("=B3", "=B2")]
    [TestCase("=sum(B1:C3)", "=sum(B1:C2)")]
    [TestCase("=sum(A1:C5)", "=sum(A1:C5)")]
    [TestCase("=D3", "=D3")]
    public void Remove_Cells_Shift_Up_Adjusts_Only_References_Within_Band(string formula, string expected)
    {
        RemoveRows(formula, BandAtRow2).Should().Be(expected);
    }

    [TestCase("=B2+sum(A1:C5)+sum(B:B)+sum(2:2)+$C$3+Sheet2!B2")]
    [TestCase("=sum(A2:C2)+D10")]
    public void Null_Band_Matches_Whole_Axis_Insert_And_Remove(string formula)
    {
        foreach (var axis in new[] { Axis.Row, Axis.Col })
        {
            var expectedInsert = Parse(formula);
            expectedInsert.InsertRowColIntoReferences(1, 2, axis, "Sheet1");
            var actualInsert = Parse(formula);
            actualInsert.InsertRowColIntoReferences(1, 2, axis, "Sheet1", band: null);
            actualInsert.ToFormulaString().Should().Be(expectedInsert.ToFormulaString());

            var expectedRemove = Parse(formula);
            expectedRemove.RemoveRowColFromReferences(1, 2, axis, "Sheet1");
            var actualRemove = Parse(formula);
            actualRemove.RemoveRowColFromReferences(1, 2, axis, "Sheet1", band: null);
            actualRemove.ToFormulaString().Should().Be(expectedRemove.ToFormulaString());
        }
    }
}
