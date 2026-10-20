namespace BlazorDatasheet.Formula.Core.Interpreter.References;

/// <summary>
/// The part of a table that a structured reference refers to, e.g. #Headers or #This Row (@).
/// </summary>
public enum TableItemSpecifier
{
    All,
    Data,
    Headers,
    Totals,
    ThisRow
}

/// <summary>
/// The parsed form of a structured reference such as Sales[Amount], [@Qty] or Sales[[#Headers],[C1]:[C2]].
/// Names are compared case-insensitively and <see cref="Items"/> is compared as a set.
/// </summary>
/// <param name="TableName">The table name, or null for an unqualified reference like [@Col].</param>
/// <param name="Items">The item specifiers. Empty means the data area.</param>
/// <param name="ColumnStart">The first (or only) column, or null when the whole width is referenced.</param>
/// <param name="ColumnEnd">The last column of a column span, or null.</param>
public sealed record StructuredReferenceDescriptor(
    string? TableName,
    IReadOnlyList<TableItemSpecifier> Items,
    string? ColumnStart,
    string? ColumnEnd)
{
    /// <summary>
    /// Whether the reference is to the row of the calling cell (@ or #This Row).
    /// </summary>
    public bool IsThisRow => Items.Contains(TableItemSpecifier.ThisRow);

    public bool HasColumns => ColumnStart != null;

    public StructuredReferenceDescriptor WithTableName(string? tableName) => this with { TableName = tableName };

    public StructuredReferenceDescriptor WithColumnNames(string? columnStart, string? columnEnd) =>
        this with { ColumnStart = columnStart, ColumnEnd = columnEnd };

    public bool Equals(StructuredReferenceDescriptor? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(TableName, other.TableName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(ColumnStart, other.ColumnStart, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(ColumnEnd, other.ColumnEnd, StringComparison.OrdinalIgnoreCase) &&
               GetItemMask(Items) == GetItemMask(other.Items);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            TableName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(TableName),
            ColumnStart == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(ColumnStart),
            ColumnEnd == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(ColumnEnd),
            GetItemMask(Items));
    }

    public override string ToString() => StructuredReferenceSyntax.Format(this);

    private static int GetItemMask(IReadOnlyList<TableItemSpecifier> items)
    {
        var mask = 0;
        foreach (var item in items)
            mask |= 1 << (int)item;
        return mask;
    }
}
