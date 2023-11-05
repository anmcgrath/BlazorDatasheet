using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;


public class ColReference : Reference
{
    public int ColNumber { get; }
    public bool IsFixedReference { get; }

    public ColReference(int colNumber, bool isFixedReference)
    {
        ColNumber = colNumber;
        IsFixedReference = isFixedReference;
    }

    public override ReferenceKind Kind => ReferenceKind.Column;

    public override string ToRefText()
    {
        return (IsFixedReference ? "$" : "") + RangeText.ColNumberToLetters(ColNumber);
    }

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == Kind &&
               ((ColReference)reference).ColNumber == ColNumber;
    }
}