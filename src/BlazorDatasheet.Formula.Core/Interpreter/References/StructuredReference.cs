using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core.Interpreter.References;

/// <summary>
/// A structured reference to part of a table, e.g. Sales[Amount], [@Qty] or Sales[[#Headers],[C1]:[C2]].
/// The reference is described by its <see cref="Descriptor"/> rather than by cell positions, so it is never
/// shifted or moved with the cells - the table itself is. It is resolved to a region against the table
/// it names; until then <see cref="Region"/> is an <see cref="EmptyRegion"/>.
/// </summary>
public class StructuredReference : Reference
{
    /// <summary>
    /// What the reference refers to. Internally settable so that table and column renames can update the
    /// reference in place; the formula text is derived from it.
    /// </summary>
    public StructuredReferenceDescriptor Descriptor { get; internal set; }

    /// <summary>
    /// The table name as written, or null for an unqualified reference like [@Col].
    /// </summary>
    public string? TableName => Descriptor.TableName;

    public string? ColumnStart => Descriptor.ColumnStart;

    public string? ColumnEnd => Descriptor.ColumnEnd;

    public IReadOnlyList<TableItemSpecifier> Items => Descriptor.Items;

    /// <summary>
    /// The name of the table the reference was last resolved against. For an unqualified reference this
    /// is the table containing the formula. Null when the reference has not been resolved against a table; it can
    /// be set while <see cref="IsResolved"/> is false, when the table was found but the reference doesn't resolve
    /// to cells (e.g. an error, or the headers of a sheet table).
    /// </summary>
    public string? ResolvedTableName { get; internal set; }

    /// <summary>
    /// Whether the reference has been resolved to a region of a table.
    /// </summary>
    public bool IsResolved => Region is not EmptyRegion;

    public StructuredReference(StructuredReferenceDescriptor descriptor)
    {
        Descriptor = descriptor;
    }

    public override ReferenceKind Kind => ReferenceKind.Structured;

    /// <summary>
    /// The canonical structured reference text. There is no sheet prefix because tables are named
    /// workbook-wide.
    /// </summary>
    public override string ToAddressText() => StructuredReferenceSyntax.Format(Descriptor);

    public override bool SameAs(Reference reference)
    {
        return reference.Kind == ReferenceKind.Structured &&
               ((StructuredReference)reference).Descriptor.Equals(Descriptor);
    }

    internal override void Shift(int offsetRow, int offsetCol)
    {
    }

    internal override void Move(int offsetRow, int offsetCol)
    {
    }

    /// <summary>
    /// Always false. A structured reference that can't be resolved evaluates to #REF! but stays valid,
    /// because an invalid reference is written as #REF! and the structured reference text would be lost
    /// when the formula is cloned or serialized.
    /// </summary>
    public override bool IsInvalid
    {
        get => false;
        protected set { }
    }

    public override IRegion Region { get; protected set; } = new EmptyRegion();

    /// <summary>
    /// Stores the region the reference resolved to. Unlike a <see cref="NamedReference"/> the region is
    /// kept, so dependency tracking can be restored from it.
    /// </summary>
    internal override void SetRegion(IRegion region)
    {
        Region = region;
    }
}
