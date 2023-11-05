namespace BlazorDatasheet.Formula.Core.Interpreter.References;

public class RowReference : Reference
{
    public int RowNumber { get; }
    public bool IsFixedReference { get; }

    public RowReference(int rowNumber, bool isFixedReference)
    {
        RowNumber = rowNumber;
        IsFixedReference = isFixedReference;
    }

    public override ReferenceKind Kind => ReferenceKind.Row;

    public override string ToRefText()
    {
        return (IsFixedReference ? "$" : "") + (RowNumber + 1);
    }

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == Kind &&
               ((RowReference)reference).RowNumber == RowNumber;
    }
}