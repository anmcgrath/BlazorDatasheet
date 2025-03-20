using System.Globalization;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.Lexing;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SheetCultureTests
{
    [Test]
    public void Specify_Culture_Options_In_Sheet_Formula_Options_Will_Eval_Correctly()
    {
        var sheet = new Sheet(10, 10, 5, 5, new FormulaOptions()
        {
            SeparatorSettings = new SeparatorSettings(new CultureInfo("fr-FR"))
        });
        sheet.Cells.SetFormula(0, 0, "=sum(1,2; 2)");
        sheet.Cells.GetValue(0, 0).Should().Be(3.2);
    }

    [Test]
    public void Specify_Culture_Options_In_Workbook_Formula_Options_Will_Eval_Correctly()
    {
    }
}