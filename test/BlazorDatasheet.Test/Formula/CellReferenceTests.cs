using System;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.Parsing;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class CellReferenceTests
{
    private SyntaxTree Parse(string str)
    {
        return new Parser().Parse(str);
    }

    [Test]
    public void Range_Same_As()
    {
        var r1 = new RangeReference(new CellAddress(0, 0), new CellAddress(1, 1));
        var r2 = new RangeReference(new CellAddress(0, 0), new CellAddress(1, 1));
        Assert.True(r1.SameAs(r2));
        Assert.True(r2.SameAs(r1));

        var r3 = new RangeReference(new ColAddress(0, true), new ColAddress(0, true));
        var r4 = new RangeReference(new ColAddress(0, true), new ColAddress(0, true));
        var r5 = new RangeReference(new ColAddress(0, true), new ColAddress(1, true));

        Assert.True(r3.SameAs(r4));
        Assert.False(r4.SameAs(r5));

        var r6 = new RangeReference(new RowAddress(0, true), new RowAddress(0, true));
        var r7 = new RangeReference(new RowAddress(0, true), new RowAddress(0, true));
        var r8 = new RangeReference(new RowAddress(0, true), new RowAddress(1, true));

        Assert.True(r6.SameAs(r7));
        Assert.False(r8.SameAs(r7));

        var r9 = new RangeReference(new CellAddress(1, 1), new CellAddress(0, 0));
        var r10 = new RangeReference(new CellAddress(0, 0), new CellAddress(1, 1));
        Assert.True(r9.SameAs(r10));
    }

    [Test]
    public void Cell_Same_As()
    {
        var c1 = new CellReference(10, 10, false, true);
        var c2 = new CellReference(10, 10, true, false);
        var c3 = new CellReference(11, 10, false, true);
        var c4 = new CellReference(10, 11, false, true);

        Assert.True(c1.SameAs(c2));
        Assert.False(c1.SameAs(c3));
        Assert.False(c1.SameAs(c4));
    }

    [Test]
    [TestCase("$A1:A2", true)]
    [TestCase("B$2:$A1$", false)]
    [TestCase("namedRange", true)]
    [TestCase("namedRange$", false)]
    [TestCase("$2:$3", true)]
    [TestCase("$C:$D", true)]
    [TestCase("C:D$", false)]
    [TestCase("A:B:C", false)]
    public void Parse_Ranges(string refStr, bool isValid)
    {
        var formulaEngine = new FormulaEngine(new Sheet(1, 1));
        var refCellValue = formulaEngine.Evaluate(formulaEngine.ParseFormula($"={refStr}"), resolveReferences: false);
        var isReferenceType = refCellValue.ValueType == CellValueType.Reference;
        isReferenceType.Should().Be(isValid);

        if (isReferenceType)
        {
            var reference = (Reference)refCellValue.Data!;
            reference.ToAddressText().Should().Be(refStr);
        }
    }

    [Test]
    public void Sheet_Ref_Parsed_With_Quoted_Sheet_Name()
    {
        var str = "='Sheet1'!A1:A2";
        var parsedRef = Parse(str);
        parsedRef.Errors.Should().BeEmpty();
        parsedRef.Root.Should().BeOfType<ReferenceExpression>();
        ((ReferenceExpression)parsedRef.Root).Reference.SheetName.Should().Be("Sheet1");
    }
    
    [Test]
    public void Sheet_Ref_Parsed_With_NonQuoted_Sheet_Name()
    {
        var str = "=Sheet1!A1:A2";
        var parsedRef = Parse(str);
        parsedRef.Errors.Should().BeEmpty();
        parsedRef.Root.Should().BeOfType<ReferenceExpression>();
        ((ReferenceExpression)parsedRef.Root).Reference.SheetName.Should().Be("Sheet1");
    }
    
    [Test]
    public void Sheet_Ref_Parsed_With_Ref_Before_Second_Cell()
    {
        var str = "=A1:Sheet1!A2";
        var parsedRef = Parse(str);
        parsedRef.Errors.Should().BeEmpty();
        parsedRef.Root.Should().BeOfType<ReferenceExpression>();
        ((ReferenceExpression)parsedRef.Root).Reference.SheetName.Should().Be("Sheet1");
    }
}