namespace BlazorDatasheet.DataStructures.Graph;

/// <summary>
/// Identifies a vertex in a <see cref="DependencyGraph{T}"/>.
/// </summary>
/// <remarks>
/// A cell vertex is identified by its sheet name together with its row and column; a named vertex
/// (a defined name) is identified by the name alone and uses <see cref="NamedIndex"/> for both.
/// </remarks>
public readonly struct VertexKey : IEquatable<VertexKey>
{
    /// <summary>
    /// The row and column of a named vertex, which has no position on a sheet.
    /// </summary>
    public const int NamedIndex = -1;

    /// <summary>
    /// The sheet name for a cell vertex, or the defined name for a named vertex.
    /// </summary>
    public readonly string Name;

    public readonly int Row;
    public readonly int Col;

    private readonly int _hash;

    private VertexKey(string? name, int row, int col)
    {
        Name = name ?? string.Empty;
        Row = row;
        Col = col;
        _hash = HashCode.Combine(Name, row, col);
    }

    public static VertexKey ForCell(int row, int col, string sheetName) => new(sheetName, row, col);

    public static VertexKey ForName(string name) => new(name, NamedIndex, NamedIndex);

    public bool IsNamed => Row == NamedIndex && Col == NamedIndex;

    /// <summary>
    /// A bare string can only refer to a defined name
    /// </summary>
    public static implicit operator VertexKey(string name) => ForName(name);

    public bool Equals(VertexKey other) =>
        Row == other.Row && Col == other.Col && string.Equals(Name, other.Name, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is VertexKey other && Equals(other);

    public override int GetHashCode() => _hash;

    public static bool operator ==(VertexKey left, VertexKey right) => left.Equals(right);

    public static bool operator !=(VertexKey left, VertexKey right) => !left.Equals(right);

    public override string ToString() => IsNamed ? Name : $"'{Name}'!R{Row}C{Col}";
}
