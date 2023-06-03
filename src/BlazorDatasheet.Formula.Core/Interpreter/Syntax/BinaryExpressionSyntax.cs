namespace BlazorDatasheet.Formula.Core.Interpreter.Syntax;

public sealed class BinaryExpressionSyntax : ExpressionSyntax
{
    public BinaryExpressionSyntax(ExpressionSyntax left, SyntaxToken operatorToken, ExpressionSyntax right)
    {
        Left = left;
        OperatorToken = operatorToken;
        Right = right;
    }

    public ExpressionSyntax Left { get; }
    public SyntaxToken OperatorToken { get; }
    public ExpressionSyntax Right { get; }

    public override SyntaxKind Kind => SyntaxKind.BinaryExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Left;
        yield return OperatorToken;
        yield return Right;
    }

    public override string ToExpressionText()
    {
        return $"{Left.ToExpressionText()}{OperatorToken.Text}{Right.ToExpressionText()}";
    }
}