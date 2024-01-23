using System;
using System.Linq;
using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class CellReferenceTests
{
    [Test]
    [TestCase("A1", 0, 0, false, false)]
    [TestCase("B3", 2, 1, false, false)]
    [TestCase("$C4", 3, 2, false, true)]
    [TestCase("A$1", 0, 0, true, false)]
    [TestCase("$A$1", 0, 0, true, true)]
    [TestCase("AA102", 101, 26, false, false)]
    [TestCase("AAA102", 101, 702, false, false)]
    public void Cell_Str_Parses_Correctly(string cellStr, int rowExpected, int colExpected, bool rowAbsExpected,
        bool colAbsExpected)
    {
        var parsed = RangeText2.TryParseSingleCellReference(cellStr, out var refr);
        parsed.Should().Be(true);
        var cellRef = (CellReference)refr!;
        rowExpected.Should().Be(cellRef.Row.RowNumber);
        colExpected.Should().Be(cellRef.Col.ColNumber);
        rowAbsExpected.Should().Be(cellRef.Row.IsFixedReference);
        colAbsExpected.Should().Be(cellRef.Col.IsFixedReference);
    }

    [Test]
    public void Range_Same_As()
    {
        var r1 = new RangeReference(new CellReference(0, 0), new CellReference(1, 1));
        var r2 = new RangeReference(new CellReference(0, 0), new CellReference(1, 1));
        Assert.True(r1.SameAs(r2));
        Assert.True(r2.SameAs(r1));

        var r3 = new RangeReference(new ColReference(0, true), new ColReference(0, true));
        var r4 = new RangeReference(new ColReference(0, true), new ColReference(0, true));
        var r5 = new RangeReference(new ColReference(0, true), new ColReference(1, true));

        Assert.True(r3.SameAs(r4));
        Assert.False(r4.SameAs(r5));

        var r6 = new RangeReference(new RowReference(0, true), new RowReference(0, true));
        var r7 = new RangeReference(new RowReference(0, true), new RowReference(0, true));
        var r8 = new RangeReference(new RowReference(0, true), new RowReference(1, true));

        Assert.True(r6.SameAs(r7));
        Assert.False(r8.SameAs(r7));

        var r9 = new RangeReference(new CellReference(1, 1), new CellReference(0, 0));
        var r10 = new RangeReference(new CellReference(0, 0), new CellReference(1, 1));
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
    [TestCase("$A1:A2", true, false)]
    [TestCase("B$2:$A1$", false, false)]
    [TestCase("namedRange", true, true)]
    [TestCase("namedRange$", false, false)]
    [TestCase("$2:$3", true, false)]
    [TestCase("$C:$D", true, false)]
    [TestCase("C:D$", false, false)]
    [TestCase("A:B:C", true, false)]
    public void Parse_Ranges(string refStr, bool isValid, bool isNamedRef)
    {
        var res1 = RangeText2.TryParseReference(refStr.AsSpan(), out var reference);
        res1.Should().Be(isValid);

        if (!isValid)
            return;

        if (isNamedRef)
            reference.Should().BeOfType<NamedReference>();
        else
        {
            reference.Should().NotBeOfType<NamedReference>();
        }

        var resultStr = reference!.ToRefText();
        resultStr.Should().Be(refStr);
    }
}