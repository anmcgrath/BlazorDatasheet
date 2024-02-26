using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class BinaryOperationExpression : Expression
{
    public Expression Left { get; }
    public Token OpToken { get; }
    public Expression Right { get; }

    public BinaryOperationExpression(Expression left, Token opToken, Expression right)
    {
        Left = left;
        OpToken = opToken;
        Right = right;
    }

    public override NodeKind Kind => NodeKind.BinaryOperation;

    public override string ToExpressionText()
    {
        return Left.ToExpressionText() + OpToken.Text + Right.ToExpressionText();
    }
}