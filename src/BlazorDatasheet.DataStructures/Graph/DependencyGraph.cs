namespace BlazorDatasheet.DataStructures.Graph;

public class DependencyGraph<T> where T : Vertex
{
    /// <summary>
    /// Adjacency list - specifies directed edges between vertices
    /// Note this is a dictionary of a dictionary
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
    /// Return the vertices adjacent (vertices that depend on v) to Vertex v
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IEnumerable<T> Adj(string key)
    {
        var isPresent = _symbolTable.ContainsKey(key);
        if (!isPresent)
            return Enumerable.Empty<T>();
        return _adj[key].Values;
    }

    public T GetVertex(string key)
    {
        return _symbolTable[key];
    }

    /// <summary>
    /// Return the vertices adjacent (vertices that depend on v) to Vertex v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<T> Adj(T v) => Adj(v.Key);

    /// <summary>
    /// Return the precedent vertices (vertices that are dependent on by v) to Vertex v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<T> Prec(T v) => Prec(v.Key);

    /// <summary>
    /// Return the precedent vertices (vertices that are dependent on by v) to Vertex v
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public IEnumerable<T> Prec(string key)
    {
        var isPresent = _symbolTable.ContainsKey(key);
        if (!isPresent)
            return Enumerable.Empty<T>();
        return _prec[key].Values;
    }


    /// <summary>
    /// Removes the Vertex v and any associated edges
    /// </summary>
    /// <param name="v"></param>
    /// <param name="clearNoEdges">Whether to remove any vertices that are left with no edges</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveVertex(T v, bool clearNoEdges = true)
    {
        if (!_symbolTable.ContainsKey(v.Key))
            return;
        var adj = Adj(v);
        var prec = Prec(v);

        // Remove edges
        foreach (var w in adj)
        {
            RemoveEdge(v, w, clearNoEdges);
        }

        foreach (var w in prec)
        {
            RemoveEdge(w, v, clearNoEdges);
        }

        // the vertex may have been removed as part of removing edges
        if (_symbolTable.ContainsKey(v.Key))
        {
            _symbolTable.Remove(v.Key);
            _numVertices--;
            _adj.Remove(v.Key);
            _prec.Remove(v.Key);
        }
    }

    /// <summary>
    /// Removes the edge that links v -> w (not this is not an associative operation)
    /// If there is an edge between w -> v it is not removed.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="w"></param>
    /// <param name="clearIfNoDependents">If set to true, clears any vertices if they are left with no dependents.</param>
    public void RemoveEdge(T v, T w, bool clearIfNoDependents = true)
    {
        if (_symbolTable.ContainsKey(v.Key))
        {
            if (_adj[v.Key].ContainsKey(w.Key))
            {
                _adj[v.Key].Remove(w.Key);
                _prec[w.Key].Remove(v.Key);
                _numEdges--;

                if (clearIfNoDependents)
                {
                    RemoveIfNoDependents(v);
                    RemoveIfNoDependents(w);
                }
            }
        }
    }

    private void RemoveIfNoDependents(T v)
    {
        if (!IsDependedOn(v) && !IsDependentOnAny(v))
        {
            RemoveVertex(v, false);
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
    /// Swaps out the existing Vertex with a new Vertex
    /// </summary>
    /// <param name="existing"></param>
    /// <param name="newVertex"></param>
    public void Swap(T existing, T newVertex)
    {
        if (_symbolTable.ContainsKey(existing.Key))
        {
            var dependedOnBy = Adj(existing).ToList();
            var dependents = Prec(existing).ToList();
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
    /// <param name="v"></param>
    /// <returns></returns>
    public bool IsDependedOn(T v)
    {
        return Adj(v).Any();
    }

    public bool IsDependentOnAny(T v)
    {
        return Prec(v).Any();
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
}

public class DependencyGraph : DependencyGraph<Vertex>
{
}