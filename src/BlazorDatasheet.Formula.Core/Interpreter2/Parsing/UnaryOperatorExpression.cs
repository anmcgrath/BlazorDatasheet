using BlazorDatasheet.Formula.Core.Interpreter2.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter2.Parsing;

public class UnaryOperatorExpression : Expression
{
    public override NodeKind Kind => NodeKind.UnaryOperation;

    public Token OperatorToken { get; }
    public Expression Expression { get; }

    public UnaryOperatorExpression(Token operatorToken, Expression expression)
    {
        OperatorToken = operatorToken;
        Expression = expression;
    }
    public override string ToExpressionText()
    {
        return OperatorToken.Tag + Expression.ToExpressionText();
    }
}