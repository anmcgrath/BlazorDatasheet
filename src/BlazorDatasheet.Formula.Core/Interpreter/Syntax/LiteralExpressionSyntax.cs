namespace BlazorDatasheet.Formula.Core.Interpreter.Syntax;

public sealed class LiteralExpressionSyntax : ExpressionSyntax
{
    public LiteralExpressionSyntax(SyntaxToken literalToken) : this(literalToken, literalToken.Value)
    {
    }

    public LiteralExpressionSyntax(SyntaxToken literalToken, object value)
    {
        LiteralToken = literalToken;
        Value = value;
    }

    public SyntaxToken LiteralToken { get; }
    public object Value { get; }

    public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return LiteralToken;
    }

    public override string ToExpressionText()
    {
        return LiteralToken.Text;
    }
}