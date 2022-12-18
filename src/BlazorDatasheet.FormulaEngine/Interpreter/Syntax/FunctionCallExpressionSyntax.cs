using System.Text;

namespace BlazorDatasheet.FormulaEngine.Interpreter.Syntax;

public class FunctionCallExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken Identifier { get; }
    public IReadOnlyList<ExpressionSyntax> Args { get; }

    public FunctionCallExpressionSyntax(SyntaxToken identifier, IReadOnlyList<ExpressionSyntax> args)
    {
        Identifier = identifier;
        Args = args;
    }

    public override SyntaxKind Kind => SyntaxKind.FunctionCallExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return Identifier;
        foreach (var arg in Args)
            yield return arg;
    }

    public override string ToExpressionText()
    {
        var strBuilder = new StringBuilder();
        strBuilder.Append(Identifier.Text);
        strBuilder.Append("(");
        strBuilder.Append(string.Join(",", Args.Select(x => x.ToExpressionText())));
        strBuilder.Append(")");
        return strBuilder.ToString();
    }
}