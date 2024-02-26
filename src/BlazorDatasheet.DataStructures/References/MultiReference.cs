using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.DataStructures.References;

public class MultiReference : Reference
{
    public override ReferenceKind Kind => ReferenceKind.Multiple;
    public Reference[] References { get; private set; }

    public MultiReference(Reference[] references)
    {
        References = references;
    }

    public override bool SameAs(Reference reference)
    {
        return reference.ToString() == this.ToString();
    }

    public override void Shift(int offsetRow, int offsetCol)
    {
        foreach (var reference in References)
            reference.Shift(offsetRow, offsetCol);
    }

    public override IRegion ToRegion()
    {
        return new AllRegion();
    }

    public override string ToRefText()
    {
        return string.Join(":", References.Select(x => x.ToRefText()));
    }
}