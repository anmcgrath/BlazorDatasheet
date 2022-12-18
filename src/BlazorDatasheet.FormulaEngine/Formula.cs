using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;
using ExpressionEvaluator.CodeAnalysis.Types;

namespace BlazorDatasheet.FormulaEngine;

public class Formula
{
    internal SyntaxTree ExpressionTree;
    public IEnumerable<Reference> References => ExpressionTree?.References;

    internal Formula(SyntaxTree expressionTree)
    {
        ExpressionTree = expressionTree;
    }
}