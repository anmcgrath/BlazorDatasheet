namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ErrorExpression : Expression
{
    public ErrorType ErrorType { get; }

    public ErrorExpression(ErrorType errorType)
    {
        ErrorType = errorType;
    }

    public override NodeKind Kind => NodeKind.Error;

    public override string ToExpressionText()
    {
        return ErrorType.ToErrorString();
    }
}