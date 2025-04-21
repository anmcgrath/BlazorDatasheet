using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class UnaryOperatorExpression : Expression
{
    private readonly bool _isPostFix;
    public override NodeKind Kind => NodeKind.UnaryOperation;

    public Token OperatorToken { get; }
    public Expression Expression { get; }

    public UnaryOperatorExpression(Token operatorToken, Expression expression, bool isPostFix = false)
    {
        _isPostFix = isPostFix;
        OperatorToken = operatorToken;
        Expression = expression;
    }

    public override string ToExpressionText()
    {
        if(_isPostFix) 
            return Expression.ToExpressionText() + OperatorToken.Text;
        else
            return OperatorToken.Text + Expression.ToExpressionText();
    }
    
    public override IEnumerable<Node> GetChildren() => [Expression];
}