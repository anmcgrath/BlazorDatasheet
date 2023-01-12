using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;
using ExpressionEvaluator.CodeAnalysis.Types;

namespace BlazorDatasheet.FormulaEngine;

public class Formula
{
    internal readonly SyntaxTree ExpressionTree;
    public IEnumerable<Reference> References => ExpressionTree?.References;

    internal Formula(SyntaxTree expressionTree)
    {
        ExpressionTree = expressionTree;
    }
}