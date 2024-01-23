using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ParenthesizedExpression : Expression
{
    public Token LeftParenth { get; }
    public Expression Expression { get; }
    public Token RightParenth { get; }

    public override NodeKind Kind => NodeKind.ParenthesizedExpression;

    public ParenthesizedExpression(Token leftParenth, Expression expression, Token rightParenth)
    {
        LeftParenth = leftParenth;
        Expression = expression;
        RightParenth = rightParenth;
    }

    public override string ToExpressionText()
    {
        return "(" + this.Expression.ToExpressionText() + ")";
    }
}