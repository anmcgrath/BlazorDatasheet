using System.Text;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ArrayConstantExpression : Expression
{
    private readonly FormulaOptions _options;
    public override NodeKind Kind => NodeKind.ArrayConstant;
    public List<List<LiteralExpression>> Rows { get; }

    public ArrayConstantExpression(List<List<LiteralExpression>> rows, FormulaOptions options)
    {
        _options = options;
        Rows = rows;
    }

    public override string ToExpressionText()
    {
        var sb = new StringBuilder("{");
        sb.Append(
            string.Join(_options.SeparatorSettings.RowSeparator, Rows.Select(row =>
                string.Join(_options.SeparatorSettings.ColumnSeparator, row.Select(c => c.ToExpressionText())))));
        sb.Append('}');
        return sb.ToString();
    }
}