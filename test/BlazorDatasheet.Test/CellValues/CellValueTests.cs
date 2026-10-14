using System;
using NUnit.Framework;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Extensions;
using AwesomeAssertions;

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
    [Test]
    public void Error_Equality_Handles_The_Code_And_Message()
    {
        var value = CellValue.Error(ErrorType.Na, "missing");
        (value == CellValue.Error(ErrorType.Na, "missing")).Should().BeTrue();
        (value == CellValue.Error(ErrorType.Ref, "missing")).Should().BeFalse();
        (value == CellValue.Error(ErrorType.Na, "different")).Should().BeFalse();
        value.GetHashCode().Should().Be(CellValue.Error(ErrorType.Na, "missing").GetHashCode());
    }
}