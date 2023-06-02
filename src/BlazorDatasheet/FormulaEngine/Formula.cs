using BlazorDatasheet.FormulaEngine.Interpreter.References;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

namespace BlazorDatasheet.FormulaEngine;

public class Formula
{
    public int? Row { get; }
    public int? Col { get; }
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference>? References => ExpressionTree?.References;

    internal Formula(SyntaxTree expressionTree)
    {
        ExpressionTree = expressionTree;
    }

    public bool IsValid()
    {
        return !ExpressionTree.Diagnostics.Any();
    }

    public static bool IsFormula(string? formula)
    {
        return formula != null && formula.StartsWith('=');
    }
}