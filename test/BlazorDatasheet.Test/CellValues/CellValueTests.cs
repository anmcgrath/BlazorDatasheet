using System;
using NUnit.Framework;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Extensions;
using FluentAssertions;

namespace BlazorDatasheet.Test.CellValues;

public class CellValueTests
{
    [Test]
    public void Cell_Value_Date_Compares_With_Number()
    {
        var c1 = CellValue.Date(1.0.ToDate());
        var c2 = CellValue.Number(0);
        var c3 = CellValue.Date((-1.0).ToDate());

        c2.CompareTo(c1).Should().Be(-1);
        c2.CompareTo(c3).Should().Be(1);
    }
}