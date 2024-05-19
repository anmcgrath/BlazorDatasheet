namespace BlazorDatasheet.DataStructures.Graph;

public class DependencyGraph
{
    /// <summary>
    /// Adjacency list - specifies directed edges between vertices
    /// Note this is a dictionary of a dictionary
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, Vertex>> _adj;

    /// <summary>
    /// Precedents list - reverse of adjacency list
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, Vertex>> _prec;

    /// <summary>
    /// Maps between vertex key and Vertex
    /// </summary>
    private readonly Dictionary<string, Vertex> _symbolTable;

    private int _numVertices;
    private int _numEdges;

    /// <summary>
    /// The number of vertices in the graph
    /// </summary>
    public int V => _numVertices;

    /// <summary>
    /// The number of edges in the graph
    /// </summary>
    public int E => _numEdges;

    private readonly TopologicalSort _topo = new();

    public DependencyGraph()
    {
        _adj = new();
        _prec = new();
        _symbolTable = new Dictionary<string, Vertex>();
    }

    /// <summary>
    /// Adds vertex to the graph
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public void AddVertex(Vertex v)
    {
        if (!_symbolTable.ContainsKey(v.Key))
        {
            _symbolTable.Add(v.Key, v);
            _adj.Add(v.Key, new Dictionary<string, Vertex>());
            _prec.Add(v.Key, new Dictionary<string, Vertex>());
            _numVertices++;
        }
    }

    public IEnumerable<Vertex> GetAll() => _symbolTable.Values;

    /// <summary>
    /// Return the vertices adjacent to vertex v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<Vertex> Adj(Vertex v)
    {
        var isPresent = _symbolTable.ContainsKey(v.Key);
        if (!isPresent)
            return Enumerable.Empty<Vertex>();
        return _adj[v.Key].Values;
    }

    /// <summary>
    /// Return the precedent vertices to vertex v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<Vertex> Prec(Vertex v)
    {
        var isPresent = _symbolTable.ContainsKey(v.Key);
        if (!isPresent)
            return Enumerable.Empty<Vertex>();
        return _prec[v.Key].Values;
    }

    /// <summary>
    /// Removes the vertex v and any associated edges
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveVertex(Vertex v)
    {
        if (!_symbolTable.ContainsKey(v.Key))
            return;
        var adj = Adj(v);
        var prec = Prec(v);
        // keep a list of vertices that we removed edges to because we want to remove those if they
        // no longer have any references
        var toRemoveSet = new HashSet<string>();

        // Remove edges
        foreach (var w in adj)
        {
            RemoveEdge(v, w);
        }

        foreach (var w in prec)
        {
            RemoveEdge(w, v);
        }

        _symbolTable.Remove(v.Key);
        _adj.Remove(v.Key);
        _prec.Remove(v.Key);
        _numVertices--;
    }

    /// <summary>
    /// Removes the edge that links v -> w (not this is not an associative operation)
    /// If there is an edge between w -> v it is not removed.
    /// </summary>
    /// <param name="v"></param>
    /// <param name="w"></param>
    public void RemoveEdge(Vertex v, Vertex w)
    {
        if (_symbolTable.ContainsKey(v.Key))
        {
            if (_adj[v.Key].ContainsKey(w.Key))
            {
                _adj[v.Key].Remove(w.Key);
                _prec[w.Key].Remove(v.Key);
                _numEdges--;

                RemoveIfNoDependents(v);
                RemoveIfNoDependents(w);
            }
        }
    }

    private void RemoveIfNoDependents(Vertex v)
    {
        if (!IsDependedOn(v) && !HasDependents(v))
        {
            RemoveVertex(v);
        }
    }

    /// <summary>
    /// Adds an edge between the two vertices.
    /// If the vertices are not already present, they are added
    /// </summary>
    /// <param name="v"></param>
    /// <param name="w"></param>
    public void AddEdge(Vertex v, Vertex w)
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
    /// Whether a vertex is connected to any other vertices.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public bool IsDependedOn(Vertex v)
    {
        return Adj(v).Any();
    }

    public bool HasDependents(Vertex v)
    {
        return Prec(v).Any();
    }

    /// <summary>
    /// Adds edges between the vertex v and all vertices in the array ws
    /// </summary>
    /// <param name="v"></param>
    /// <param name="ws"></param>
    public void AddEdges(Vertex v, IEnumerable<Vertex> ws)
    {
        foreach (var w in ws)
            AddEdge(v, w);
    }

    /// <summary>
    /// Adds edges between the vertex v and all vertices in the array ws
    /// </summary>
    /// <param name="vs"></param>
    /// <param name="w"></param>
    public void AddEdges(IEnumerable<Vertex> vs, Vertex w)
    {
        foreach (var v in vs)
            AddEdge(v, w);
    }

    public IEnumerable<Vertex> TopologicalSort()
    {
        return _topo.Sort(this);
    }

    /// <summary>
    /// Checks whether a vertex with the key given exists in the graph.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool HasVertex(string key)
    {
        return _symbolTable.ContainsKey(key);
    }
}