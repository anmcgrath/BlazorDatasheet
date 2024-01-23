using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class FunctionExpression : Expression
{
    public override NodeKind Kind => NodeKind.FunctionCall;
    public IdentifierToken FunctionToken { get; }
    public List<Expression> Args { get; }

    public FunctionExpression(IdentifierToken functionToken, List<Expression> args)
    {
        FunctionToken = functionToken;
        Args = args;
    }

    public override string ToExpressionText()
    {
        return FunctionToken.Value + "(" + string.Join(",", Args.Select(x => x.ToExpressionText())) + ")";
    }
}