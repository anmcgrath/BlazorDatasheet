namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

public class ErrorExpressionSyntax : ExpressionSyntax
{
    public ErrorType ErrorType { get; }

    public ErrorExpressionSyntax(ErrorType errorType)
    {
        ErrorType = errorType;
    }

    public override SyntaxKind Kind => SyntaxKind.ErrorExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        return Enumerable.Empty<SyntaxNode>();
    }

    public override string ToExpressionText()
    {
        return ErrorType.ToString();
    }
}