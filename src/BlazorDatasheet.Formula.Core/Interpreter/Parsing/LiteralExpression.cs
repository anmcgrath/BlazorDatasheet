namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class LiteralExpression : Expression
{
    public override NodeKind Kind => NodeKind.Literal;
    public CellValue Value { get; }

    public LiteralExpression(CellValue value)
    {
        Value = value;
    }

    public override string ToExpressionText()
    {
        return Value.ToString();
    }
}