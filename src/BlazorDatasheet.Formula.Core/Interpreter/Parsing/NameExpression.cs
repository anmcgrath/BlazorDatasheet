using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class NameExpression : Expression
{
    public override NodeKind Kind => NodeKind.Name;
    public IdentifierToken NameToken { get; }

    public NameExpression(IdentifierToken nameToken)
    {
        NameToken = nameToken;
    }
    public override string ToExpressionText()
    {
        return NameToken.Value;
    }
}