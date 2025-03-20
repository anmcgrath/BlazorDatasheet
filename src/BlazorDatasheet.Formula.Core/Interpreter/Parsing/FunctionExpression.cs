using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class FunctionExpression : Expression
{
    public override NodeKind Kind => NodeKind.FunctionCall;
    public IdentifierToken FunctionToken { get; }
    public List<Expression> Args { get; }

    public ISheetFunction? Function { get; }

    public bool FunctionExists => Function != null;
    private char _parameterSeparator;

    public FunctionExpression(IdentifierToken functionToken, List<Expression> args, ISheetFunction? function,
        char parameterSeparator)
    {
        FunctionToken = functionToken;
        Args = args;
        Function = function;
        _parameterSeparator = parameterSeparator;
    }

    public override string ToExpressionText()
    {
        return FunctionToken.Value + "(" + string.Join(",", Args.Select(x => x.ToExpressionText())) + ")";
    }
}