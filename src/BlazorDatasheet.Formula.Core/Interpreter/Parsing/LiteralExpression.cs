namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class LiteralExpression : Expression
{
    public override NodeKind Kind => NodeKind.Literal;
    public CellValue Value { get; }
    public override IEnumerable<Node> GetChildren() => [];

    public LiteralExpression(CellValue value)
    {
        Value = value;
    }

    public override string ToExpressionText()
    {
        if (Value.Data is FormulaError error)
            return error.ErrorType.ToErrorString();
        return Value.ToString();
    }
}
