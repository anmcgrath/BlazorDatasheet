using BlazorDatasheet.FormulaEngine.Interpreter.References;

namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

public class RangeReferenceExpressionSyntax : ExpressionSyntax
{
    public RangeReference Reference { get; }

    public RangeReferenceExpressionSyntax(RangeReference reference)
    {
        Reference = reference;
    }

    public override SyntaxKind Kind => SyntaxKind.RangeReferenceExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        return Enumerable.Empty<SyntaxNode>();
    }

    public override string ToExpressionText()
    {
        return Reference.ToRefText();
    }
}