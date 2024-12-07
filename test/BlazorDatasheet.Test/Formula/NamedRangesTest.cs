using System;
using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Formula;

public class NamedRangesTest
{
    [Test]
    public void Setting_Named_Range_Requires_Fully_Qualified_Sheet_Name()
    {
        throw new NotImplementedException();
    }

    [Test]
    public void Set_Named_Range_Calculates_In_Function_Correctly()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValues(0, 0, [[1], [2]]);
        sheet.FormulaEngine.SetVariable("x", "=A1:A2");
        sheet.Cells.SetFormula(2, 2, "=sum(x)");
        sheet.Cells.GetValue(2, 2).Should().Be(3);
    }
}