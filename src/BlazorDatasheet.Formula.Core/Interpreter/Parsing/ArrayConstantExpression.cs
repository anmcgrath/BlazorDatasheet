using System.Text;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ArrayConstantExpression : Expression
{
    public override NodeKind Kind => NodeKind.ArrayConstant;
    public List<List<LiteralExpression>> Rows { get; }
    private char _columnSeparator;
    private char _rowSeparator;

    public ArrayConstantExpression(List<List<LiteralExpression>> rows, char columnSeparator, char rowSeparator)
    {
        Rows = rows;
        _columnSeparator = columnSeparator;
        _rowSeparator = rowSeparator;
    }

    public override string ToExpressionText()
    {
        var sb = new StringBuilder("{");
        sb.Append(string.Join(_rowSeparator,
            Rows.Select(row => string.Join(_columnSeparator, row.Select(c => c.ToExpressionText())))));
        sb.Append('}');
        return sb.ToString();
    }
}