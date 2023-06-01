namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

public sealed class NameExpressionSyntax : ExpressionSyntax
{
    public NameExpressionSyntax(SyntaxToken identifierToken)
    {
        IdentifierToken = identifierToken;
    }

    public SyntaxToken IdentifierToken { get; }
    public override SyntaxKind Kind => SyntaxKind.NameExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return IdentifierToken;
    }

    public override string ToExpressionText()
    {
        return IdentifierToken.Text;
    }
}