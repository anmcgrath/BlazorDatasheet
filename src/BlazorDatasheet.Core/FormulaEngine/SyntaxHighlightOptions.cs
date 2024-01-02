namespace BlazorDatasheet.Core.FormulaEngine;

public class SyntaxHighlightOptions
{
    public List<System.Drawing.Color> RangeColors { get; set; } = new()
    {
        System.Drawing.Color.DodgerBlue,
        System.Drawing.Color.Red,
        System.Drawing.Color.MediumPurple,
        System.Drawing.Color.ForestGreen,
    };
}