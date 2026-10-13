namespace BlazorDatasheet.Formula.Core.Interpreter.References;

/// <summary>
/// A reference such as A1# to the cells the array formula at A1 has spilled into. It is a cell
/// reference to the anchor for everything structural - shifting, moving, dependency tracking - and is
/// widened to the anchor's current spill region when it is evaluated.
/// </summary>
public class SpillRangeReference : CellReference
{
    public SpillRangeReference(int rowIndex, int colIndex, bool isColFixed, bool isRowFixed)
        : base(rowIndex, colIndex, isColFixed, isRowFixed)
    {
    }

    public override ReferenceKind Kind => ReferenceKind.SpillRange;

    public override string ToAddressText() => base.ToAddressText() + "#";

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == ReferenceKind.SpillRange &&
               ((SpillRangeReference)reference).RowIndex == RowIndex &&
               ((SpillRangeReference)reference).ColIndex == ColIndex;
    }
}
