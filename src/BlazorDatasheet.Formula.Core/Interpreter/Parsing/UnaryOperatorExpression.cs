using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

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
        return OperatorToken.Text + Expression.ToExpressionText();
    }
}