using BlazorDatasheet.DataStructures.References;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter2.Parsing;

public class ReferenceExpression : Expression
{
    public Reference Reference { get; }

    public ReferenceExpression(Reference reference)
    {
        Reference = reference;
    }

    public override string ToExpressionText()
    {
        return Reference.ToRefText();
    }

    public override NodeKind Kind => NodeKind.Range;
}