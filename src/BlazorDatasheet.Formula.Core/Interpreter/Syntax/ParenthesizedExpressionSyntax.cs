namespace BlazorDatasheet.Formula.Core.Interpreter.Syntax;

public sealed class ParenthesizedExpressionSyntax : ExpressionSyntax
{
    public ParenthesizedExpressionSyntax(SyntaxToken openParenthesisToken, ExpressionSyntax expressionSyntax,
        SyntaxToken closeParenthesisToken)
    {
        OpenParenthesisToken = openParenthesisToken;
        ExpressionSyntax = expressionSyntax;
        CloseParenthesisToken = closeParenthesisToken;
    }

    public SyntaxToken OpenParenthesisToken { get; }
    public ExpressionSyntax ExpressionSyntax { get; }
    public SyntaxToken CloseParenthesisToken { get; }

    public override SyntaxKind Kind => SyntaxKind.ParenthesizedExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return OpenParenthesisToken;
        yield return ExpressionSyntax;
        yield return CloseParenthesisToken;
    }

    public override string ToExpressionText()
    {
        return "(" + ExpressionSyntax.ToExpressionText() + ")";
    }
}