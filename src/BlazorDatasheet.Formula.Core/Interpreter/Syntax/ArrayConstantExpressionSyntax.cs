using System.Text;

namespace BlazorDatasheet.Formula.Core.Interpreter.Syntax;

public class ArrayConstantExpressionSyntax : ExpressionSyntax
{
    public SyntaxToken ArrayToken { get; }
    public IReadOnlyList<IReadOnlyList<ExpressionSyntax>> Rows { get; }
    public override SyntaxKind Kind => SyntaxKind.ArrayConstantExpression;

    public ArrayConstantExpressionSyntax(IReadOnlyList<IReadOnlyList<ExpressionSyntax>> rows)
    {
        Rows = rows;
    }

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        foreach (var row in Rows)
        {
            foreach (var col in row)
                yield return col;
        }
    }

    public override string ToExpressionText()
    {
        var strBuilder = new StringBuilder();
        strBuilder.Append('{');
        strBuilder.Append(string.Join(";", Rows.Select(x => string.Join(",", x.Select(y => y.ToExpressionText())))));
        strBuilder.Append('}');
        return strBuilder.ToString();
    }
}