using System;
using BlazorDatasheet.Formula.Core.Interpreter.References;
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
        var cellRef = CellReference.FromString(cellStr);
        Assert.AreEqual(rowExpected, cellRef.Row.RowNumber);
        Assert.AreEqual(colExpected, cellRef.Col.ColNumber);
        Assert.AreEqual(rowAbsExpected, cellRef.Row.IsAbsoluteReference);
        Assert.AreEqual(colAbsExpected, cellRef.Col.IsAbsoluteReference);
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
}