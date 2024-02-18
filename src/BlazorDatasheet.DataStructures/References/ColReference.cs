using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.DataStructures.References;

public class ColReference : Reference
{
    public int ColNumber { get; private set; }
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

    public override void Shift(int offsetRow, int offsetCol)
    {
        if (!IsFixedReference)
            ColNumber += offsetCol;
    }

    public override IRegion ToRegion()
    {
        return new ColumnRegion(ColNumber);
    }
}