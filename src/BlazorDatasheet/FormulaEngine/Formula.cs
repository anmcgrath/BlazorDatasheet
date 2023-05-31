namespace BlazorDatasheet.FormulaEngine;

public class Formula
{
    public static bool IsFormula(string? formula)
    {
        return formula != null && formula.StartsWith('=');
    }
}