using System.Text;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ArrayConstantExpression : Expression
{
    public override NodeKind Kind => NodeKind.ArrayConstant;
    public List<List<LiteralExpression>> Rows { get; }

    public ArrayConstantExpression(List<List<LiteralExpression>> rows)
    {
        Rows = rows;
    }

    public override string ToExpressionText()
    {
        var sb = new StringBuilder("{");
        foreach (var row in Rows)
        {
            foreach (var val in row)
            {
                sb.Append(val.ToExpressionText());
            }
        }

        sb.Append('}');
        return sb.ToString();
    }
}