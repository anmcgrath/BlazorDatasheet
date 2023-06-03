namespace BlazorDatasheet.Formula.Core.Interpreter.References;


public class ColReference : Reference
{
    public int ColNumber { get; }
    public bool IsAbsoluteReference { get; }

    public ColReference(int colNumber, bool isAbsoluteReference)
    {
        ColNumber = colNumber;
        IsAbsoluteReference = isAbsoluteReference;
    }

    public override ReferenceKind Kind => ReferenceKind.Column;

    public override string ToRefText()
    {
        return (IsAbsoluteReference ? "$" : "") + CellReference.ColNumberToLetters(ColNumber);
    }

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == Kind &&
               ((ColReference)reference).ColNumber == ColNumber;
    }
}