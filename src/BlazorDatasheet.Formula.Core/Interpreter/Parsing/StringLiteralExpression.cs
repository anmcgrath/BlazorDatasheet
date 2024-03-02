namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class StringLiteralExpression : LiteralExpression
{
    public StringLiteralExpression(CellValue value) : base(value)
    {
        Console.WriteLine("str lit creaed");
    }

    public override string ToExpressionText()
    {
        return $"\"{Value}\"";
    }
}