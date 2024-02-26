using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.DataStructures.References;

public class RowReference : Reference
{
    public int RowNumber { get; private set; }
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

    public override void Shift(int offsetRow, int offsetCol)
    {
        if (!IsFixedReference)
            RowNumber += offsetRow;
    }

    public override IRegion ToRegion()
    {
        return new RowRegion(RowNumber);
    }
}