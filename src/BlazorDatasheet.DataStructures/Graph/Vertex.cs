namespace BlazorDatasheet.DataStructures.Graph;

public abstract class Vertex : IEquatable<Vertex>
{
    public abstract VertexKey Key { get; }

    public bool Equals(Vertex? other)
    {
        return other is not null && other.Key.Equals(Key);
    }

    public override bool Equals(object? obj) => obj is Vertex vertex && Equals(vertex);

    public override int GetHashCode() => Key.GetHashCode();

    public abstract void UpdateKey();
}

public abstract class Vertex<TData> : Vertex, IEquatable<Vertex<TData>>
{
    public abstract TData Data { get; }

    public bool Equals(Vertex<TData>? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return EqualityComparer<TData>.Default.Equals(Data, other.Data);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((Vertex<TData>)obj);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TData>.Default.GetHashCode(Data!);
    }
}
