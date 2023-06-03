using BlazorDatasheet.Formula.Core.Interpreter.References;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core;

public class CellFormula
{
    public int? Row { get; }
    public int? Col { get; }
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference>? References => ExpressionTree?.References;

    internal CellFormula(SyntaxTree expressionTree)
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