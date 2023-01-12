namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

public abstract class SyntaxNode
{
    public abstract SyntaxKind Kind { get; }
    public abstract IEnumerable<SyntaxNode> GetChildren();
}

public abstract class ExpressionSyntax : SyntaxNode
{
    public abstract string ToExpressionText();
}