using BlazorDatasheet.FormulaEngine.Interpreter.References;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine;

public class Formula
{
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference>? References => ExpressionTree?.References;

    internal Formula(SyntaxTree expressionTree)
    {
        ExpressionTree = expressionTree;
    }

    public static bool IsFormula(string? formula)
    {
        return formula != null && formula.StartsWith('=');
    }
}