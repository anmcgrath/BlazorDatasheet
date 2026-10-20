namespace BlazorDatasheet.Core.Data;

/// <summary>
/// What changed about a table.
/// </summary>
public enum TableChangeKind
{
    Added,
    Removed,
    Renamed,
    Resized,
    ShowTotalsChanged,
    ColumnsRenamed
}

/// <summary>
/// Describes a change to a table in <see cref="TableCollection"/>.
/// </summary>
public class TableChangedEventArgs : EventArgs
{
    /// <summary>
    /// The table's name (after the change, for a rename).
    /// </summary>
    public string Name { get; }

    public TableChangeKind Kind { get; }

    /// <summary>
    /// The table's name before it was renamed, for <see cref="TableChangeKind.Renamed"/>.
    /// </summary>
    public string? OldName { get; }

    /// <summary>
    /// The columns whose names changed, for <see cref="TableChangeKind.ColumnsRenamed"/>.
    /// </summary>
    public IReadOnlyList<(string OldName, string NewName)>? ColumnRenames { get; }

    public TableChangedEventArgs(string name, TableChangeKind kind, string? oldName = null,
        IReadOnlyList<(string OldName, string NewName)>? columnRenames = null)
    {
        Name = name;
        Kind = kind;
        OldName = oldName;
        ColumnRenames = columnRenames;
    }
}
