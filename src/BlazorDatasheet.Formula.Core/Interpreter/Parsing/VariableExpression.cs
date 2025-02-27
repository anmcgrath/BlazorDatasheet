using BlazorDatasheet.Formula.Core.Interpreter.Lexing;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class VariableExpression : Expression
{
    public override NodeKind Kind => NodeKind.Name;
    public IdentifierToken NameToken { get; }

    public VariableExpression(IdentifierToken nameToken)
    {
        NameToken = nameToken;
    }

    public override string ToExpressionText()
    {
        return NameToken.Value;
    }
}