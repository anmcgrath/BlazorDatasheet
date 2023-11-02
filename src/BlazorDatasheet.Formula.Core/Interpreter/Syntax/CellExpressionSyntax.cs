using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Syntax;


public class CellExpressionSyntax : ExpressionSyntax
{
    public CellReference CellReference { get; }
    public override SyntaxKind Kind => SyntaxKind.CellReferenceExpression;

    public CellExpressionSyntax(CellReference cellReference)
    {
        CellReference = cellReference;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        return Enumerable.Empty<SyntaxNode>();
    }

    public override string ToExpressionText()
    {
        return CellReference.ToRefText();
    }
}