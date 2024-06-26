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
        if(Reference.IsInvalid)
            return "#REF!";
        return Reference.ToAddressText();
    }

    public override NodeKind Kind => NodeKind.Range;
}