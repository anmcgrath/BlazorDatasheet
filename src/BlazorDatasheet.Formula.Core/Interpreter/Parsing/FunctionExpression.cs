using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class FunctionExpression : Expression
{
    private readonly FormulaOptions _options;
    public override NodeKind Kind => NodeKind.FunctionCall;
    public IdentifierToken FunctionToken { get; }
    public List<Expression> Args { get; }

    public ISheetFunction? Function { get; }

    public bool FunctionExists => Function != null;

    private int[] _argPositionStarts;
    private readonly Token _rightParenthToken;

    public FunctionExpression(
        IdentifierToken functionToken,
        List<Expression> args,
        ISheetFunction? function,
        FormulaOptions options,
        int[] argPositionStarts,
        Token rightParenthToken)
    {
        _options = options;
        FunctionToken = functionToken;
        Args = args;
        Function = function;
        _argPositionStarts = argPositionStarts;
        _rightParenthToken = rightParenthToken;
    }
    
    public override IEnumerable<Node> GetChildren() => Args;


    /// <summary>
    /// Returns the arg index at position <paramref name="position"/>
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public int GetArgIndex(int position)
    {
        if (position > _rightParenthToken.PositionStart)
            return -1;

        if (position < FunctionToken.PositionStart)
            return -1;

        if (_argPositionStarts.Length > 0 && position < _argPositionStarts[0])
            return -1;

        for (int i = _argPositionStarts.Length - 1; i >= 0; i--)
        {
            if (position >= _argPositionStarts[i])
                return i;
        }

        return -1;
    }

    public override string ToExpressionText()
    {
        return FunctionToken.Value + "(" + string.Join(_options.SeparatorSettings.FuncParameterSeparator,
            Args.Select(x => x.ToExpressionText())) + ")";
    }

    public bool IsPositionInsideFunc(int cursorPosition)
    {
        return cursorPosition > FunctionToken.PositionStart &&
               cursorPosition <= _rightParenthToken.PositionStart;
    }
}