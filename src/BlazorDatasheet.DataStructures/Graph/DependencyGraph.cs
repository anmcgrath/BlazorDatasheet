namespace BlazorDatasheet.DataStructures.Graph;

public class DependencyGraph<T> where T : Vertex
{
    /// <summary>
    /// Adjacency list for dependency edges (v -> w means w depends on v).
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, T>> _adj;

    /// <summary>
    /// Precedents list - reverse of adjacency list
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, T>> _prec;

    /// <summary>
    /// Maps between Vertex key and Vertex
    /// </summary>
    private readonly Dictionary<string, T> _symbolTable;

    private int _numVertices;
    private int _numEdges;

    /// <summary>
    /// The number of vertices in the graph
    /// </summary>
    public int Count => _numVertices;

    /// <summary>
    /// The number of edges in the graph
    /// </summary>
    public int E => _numEdges;

    private readonly TopologicalSort<T> _topo = new();

    public DependencyGraph()
    {
        _adj = new();
        _prec = new();
        _symbolTable = new Dictionary<string, T>();
    }

    /// <summary>
    /// Adds Vertex to the graph
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public void AddVertex(T v)
    {
        if (!_symbolTable.ContainsKey(v.Key))
        {
            _symbolTable.Add(v.Key, v);
            _adj.Add(v.Key, new Dictionary<string, T>());
            _prec.Add(v.Key, new Dictionary<string, T>());
            _numVertices++;
        }
    }

    public IEnumerable<T> GetAll() => _symbolTable.Values;

    /// <summary>
    /// Returns vertices that directly depend on the given vertex key.
    /// </summary>
    public IEnumerable<T> GetDependentsOf(string key)
    {
        var isPresent = _symbolTable.ContainsKey(key);
        if (!isPresent)
            return Enumerable.Empty<T>();
        return _adj[key].Values;
    }

    /// <summary>
    /// Returns vertices that directly depend on the given vertex.
    /// </summary>
    public IEnumerable<T> GetDependentsOf(T v) => GetDependentsOf(v.Key);

    /// <summary>
    /// Returns vertices that the given vertex depends on.
    /// </summary>
    public IEnumerable<T> GetPrecedentsOf(T v) => GetPrecedentsOf(v.Key);

    /// <summary>
    /// Returns vertices that the given vertex key depends on.
    /// </summary>
    public IEnumerable<T> GetPrecedentsOf(string key)
    {
        var isPresent = _symbolTable.ContainsKey(key);
        if (!isPresent)
            return Enumerable.Empty<T>();
        return _prec[key].Values;
    }

    /// <summary>
    /// Legacy alias. Prefer <see cref="GetDependentsOf(string)"/>.
    /// </summary>
    public IEnumerable<T> Adj(string key)
    {
        return GetDependentsOf(key);
    }

    public T? GetVertex(string key)
    {
        return _symbolTable.GetValueOrDefault(key);
    }

    /// <summary>
    /// Legacy alias. Prefer <see cref="GetDependentsOf(Vertex)"/>.
    /// </summary>
    public IEnumerable<T> Adj(T v) => Adj(v.Key);

    /// <summary>
    /// Legacy alias. Prefer <see cref="GetPrecedentsOf(Vertex)"/>.
    /// </summary>
    public IEnumerable<T> Prec(T v) => Prec(v.Key);

    /// <summary>
    /// Legacy alias. Prefer <see cref="GetPrecedentsOf(string)"/>.
    /// </summary>
    public IEnumerable<T> Prec(string key)
    {
        return GetPrecedentsOf(key);
    }


    /// <summary>
    /// Removes the Vertex v and any associated edges
    /// </summary>
    /// <param name="v"></param>
    /// <param name="clearNoEdges">Whether to remove any vertices that are left with no edges</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveVertex(T v, bool clearNoEdges = true) => RemoveVertex(v.Key, clearNoEdges);

    public void RemoveVertex(string vKey, bool clearNoEdges = true)
    {
        if (!_symbolTable.ContainsKey(vKey))
            return;
        var adj = GetDependentsOf(vKey).ToList();
        var prec = GetPrecedentsOf(vKey).ToList();

        // Remove edges
        foreach (var w in adj)
        {
            RemoveEdge(vKey, w.Key, clearNoEdges);
        }

        foreach (var w in prec)
        {
            RemoveEdge(w.Key, vKey, clearNoEdges);
        }

        // the vertex may have been removed as part of removing edges
        if (_symbolTable.ContainsKey(vKey))
        {
            _symbolTable.Remove(vKey);
            _numVertices--;
            _adj.Remove(vKey);
            _prec.Remove(vKey);
        }
    }

    /// <summary>
    /// Removes the edge that links v -> w (not this is not an associative operation)
    /// If there is an edge between w -> v it is not removed.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="w"></param>
    /// <param name="clearIfNoDependents">If set to true, clears any vertices if they are left with no dependents.</param>
    public void RemoveEdge(T v, T w, bool clearIfNoDependents = true) => RemoveEdge(v.Key, w.Key, clearIfNoDependents);

    public void RemoveEdge(string vKey, string wKey, bool clearIfNoDependents = true)
    {
        if (_symbolTable.ContainsKey(vKey))
        {
            if (_adj[vKey].ContainsKey(wKey))
            {
                _adj[vKey].Remove(wKey);
                _prec[wKey].Remove(vKey);
                _numEdges--;

                if (clearIfNoDependents)
                {
                    RemoveIfNoDependents(vKey);
                    RemoveIfNoDependents(wKey);
                }
            }
        }
    }

    private void RemoveIfNoDependents(string vKey)
    {
        if (!IsDependedOn(vKey) && !IsDependentOnAny(vKey))
        {
            RemoveVertex(vKey, false);
        }
    }

    /// <summary>
    /// Adds an edge between the two vertices.
    /// If the vertices are not already present, they are added
    /// </summary>
    /// <param name="v">Vertex v is depended on by w</param>
    /// <param name="w">Vertex w depends on v</param>
    public void AddEdge(T v, T w)
    {
        AddVertex(v);
        AddVertex(w);
        if (!_adj[v.Key].ContainsKey(w.Key))
        {
            _adj[v.Key].Add(w.Key, w);
            _prec[w.Key].Add(v.Key, v);
            _numEdges++;
        }
    }

    /// <summary>
    /// Adds edges between two vertices, only if they are already existing.
    /// </summary>
    /// <param name="vKey"></param>
    /// <param name="wKey"></param>
    public void AddEdge(string vKey, string wKey)
    {
        if (!_symbolTable.TryGetValue(vKey, out var v))
            return;
        if (!_symbolTable.TryGetValue(wKey, out var w))
            return;

        if (!_adj[vKey].ContainsKey(wKey))
        {
            _adj[vKey].Add(wKey, w);
            _prec[w.Key].Add(v.Key, v);
            _numEdges++;
        }
    }

    /// <summary>
    /// Swaps out the existing Vertex with a new Vertex
    /// </summary>
    /// <param name="existing"></param>
    /// <param name="newVertex"></param>
    public void Swap(T existing, T newVertex)
    {
        if (_symbolTable.ContainsKey(existing.Key))
        {
            var dependedOnBy = GetDependentsOf(existing).ToList();
            var dependents = GetPrecedentsOf(existing).ToList();
            RemoveVertex(existing, false);
            AddVertex(newVertex);

            foreach (var w in dependedOnBy)
                AddEdge(newVertex, w);

            foreach (var v in dependents)
                AddEdge(v, newVertex);
        }
    }

    /// <summary>
    /// Whether a Vertex is connected to any other vertices.
    /// </summary>
    /// <param name="vKey"></param>
    /// <returns></returns>
    public bool IsDependedOn(string vKey)
    {
        return GetDependentsOf(vKey).Any();
    }

    public bool IsDependentOnAny(string vKey)
    {
        return GetPrecedentsOf(vKey).Any();
    }

    /// <summary>
    /// Adds edges between the Vertex v and all vertices in the
    /// array ws
    /// </summary>
    /// <param name="v"></param>
    /// <param name="ws"></param>
    public void AddEdges(T v, IEnumerable<T> ws)
    {
        foreach (var w in ws)
            AddEdge(v, w);
    }

    /// <summary>
    /// Adds edges between the Vertex v and all vertices in the array ws
    /// </summary>
    /// <param name="vs"></param>
    /// <param name="w"></param>
    public void AddEdges(IEnumerable<T> vs, T w)
    {
        foreach (var v in vs)
            AddEdge(v, w);
    }

    public IEnumerable<T> TopologicalSort()
    {
        return _topo.Sort(this);
    }

    /// <summary>
    /// Checks whether a Vertex with the key given exists in the graph.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool HasVertex(string key)
    {
        return _symbolTable.ContainsKey(key);
    }

    /// <summary>
    /// Updates the key for the vertex after calling Vertex.UpdateKey
    /// </summary>
    /// <param name="v"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void RefreshKey(T v)
    {
        var currKey = v.Key;
        var prec = GetPrecedentsOf(currKey).Select(x => x.Key).ToList();
        var adj = GetDependentsOf(currKey).Select(x => x.Key).ToList();
        RemoveVertex(currKey, false);

        v.UpdateKey();
        AddVertex(v);

        foreach (var a in adj)
            AddEdge(v.Key, a);

        foreach (var p in prec)
            AddEdge(p, v.Key);
    }
}

public class DependencyGraph : DependencyGraph<Vertex>
{
}
