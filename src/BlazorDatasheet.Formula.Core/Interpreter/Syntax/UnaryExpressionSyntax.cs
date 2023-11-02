namespace BlazorDatasheet.Formula.Core.Interpreter.Syntax;

public sealed class UnaryExpressionSyntax : ExpressionSyntax
{
    public UnaryExpressionSyntax(SyntaxToken operatorToken, ExpressionSyntax operand)
    {
        Operand = operand;
        OperatorToken = operatorToken;
    }

    public ExpressionSyntax Operand { get; }
    public SyntaxToken OperatorToken { get; }

    public override SyntaxKind Kind => SyntaxKind.UnaryExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Operand;
    }

    public override string ToExpressionText()
    {
        return OperatorToken.Text + Operand.ToExpressionText();
    }
}