using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Parsing;

public class ReferenceExpression : Expression
{
    public Reference Reference { get; }

    public ReferenceExpression(Reference reference)
    {
        Reference = reference;
    }

    public override string ToExpressionText()
    {
        return Reference.ToAddressText();
    }

    public override NodeKind Kind => NodeKind.Range;
}