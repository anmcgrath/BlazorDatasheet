namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class StringLiteralExpression : LiteralExpression
{
    public StringLiteralExpression(CellValue value) : base(value)
    {
    }

    public override string ToExpressionText()
    {
        return $"\"{Value}\"";
    }
}