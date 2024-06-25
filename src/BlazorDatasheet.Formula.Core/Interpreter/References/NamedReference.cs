using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

public class NamedReference : Reference
{
    public string Name { get; }

    public NamedReference(string name)
    {
        Name = name;
    }

    public override ReferenceKind Kind => ReferenceKind.Named;

    public override string ToAddressText() => Name;

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == ReferenceKind.Named &&
               ((NamedReference)reference).Name == Name;
    }

    public override void Shift(int offsetRow, int offsetCol)
    {
    }

    public override bool IsInvalid { get; protected set; }
    public override IRegion Region { get; protected set; } = new EmptyRegion();
    internal override void SetRegion(IRegion region)
    {
    }
}