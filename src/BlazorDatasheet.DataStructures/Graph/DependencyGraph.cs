namespace BlazorDatasheet.DataStructures.Graph;

public class DependencyGraph<T>
{
    /// <summary>
    /// Adjacency list - specifies directed edges between vertices
    /// Note this is a dictionary of a dictionary
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, Vertex<T>>> _adj;

    /// <summary>
    /// Precedents list - reverse of adjacency list
    /// </summary>
    private readonly Dictionary<string, Dictionary<string, Vertex<T>>> _prec;

    /// <summary>
    /// Maps between Vertex<T> key and Vertex<T>
    /// </summary>
    private readonly Dictionary<string, Vertex<T>> _symbolTable;

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
        _symbolTable = new Dictionary<string, Vertex<T>>();
    }

    /// <summary>
    /// Adds Vertex<T> to the graph
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public void AddVertex<T>(Vertex<T> v)
    {
        if (!_symbolTable.ContainsKey(v.Key))
        {
            _symbolTable.Add(v.Key, v);
            _adj.Add(v.Key, new Dictionary<string, Vertex<T>>());
            _prec.Add(v.Key, new Dictionary<string, Vertex<T>>());
            _numVertices++;
        }
    }

    public IEnumerable<Vertex<T>> GetAll() => _symbolTable.Values;

    /// <summary>
    /// Return the vertices adjacent to Vertex<T> v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<Vertex<T>> Adj(string key)
    {
        var isPresent = _symbolTable.ContainsKey(key);
        if (!isPresent)
            return Enumerable.Empty<Vertex<T>>();
        return _adj[key].Values;
    }

    /// <summary>
    /// Return the vertices adjacent to Vertex<T> v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<Vertex<T>> Adj(Vertex<T> v) => Adj(v.Key);

    /// <summary>
    /// Return the precedent vertices to Vertex<T> v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<Vertex<T>> Prec(Vertex<T> v) => Prec(v.Key);

    /// <summary>
    /// Return the precedent vertices to Vertex<T> v
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public IEnumerable<Vertex<T>> Prec(string key)
    {
        var isPresent = _symbolTable.ContainsKey(key);
        if (!isPresent)
            return Enumerable.Empty<Vertex<T>>();
        return _prec[key].Values;
    }


    /// <summary>
    /// Removes the Vertex<T> v and any associated edges
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveVertex<T>(Vertex<T> v)
    {
        if (!_symbolTable.ContainsKey(v.Key))
            return;
        var adj = Adj(v);
        var prec = Prec(v);

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
    public void RemoveEdge(Vertex<T> v, Vertex<T> w)
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

    private void RemoveIfNoDependents(Vertex<T> v)
    {
        if (!IsDependedOn(v) && !HasDependents(v))
        {
            RemoveVertex<T>(v);
        }
    }

    /// <summary>
    /// Adds an edge between the two vertices.
    /// If the vertices are not already present, they are added
    /// </summary>
    /// <param name="v"></param>
    /// <param name="w"></param>
    public void AddEdge(Vertex<T> v, Vertex<T> w)
    {
        AddVertex<T>(v);
        AddVertex<T>(w);
        if (!_adj[v.Key].ContainsKey(w.Key))
        {
            _adj[v.Key].Add(w.Key, w);
            _prec[w.Key].Add(v.Key, v);
            _numEdges++;
        }
    }

    /// <summary>
    /// Whether a Vertex<T> is connected to any other vertices.
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    public bool IsDependedOn(Vertex<T> v)
    {
        return Adj(v).Any();
    }

    public bool HasDependents(Vertex<T> v)
    {
        return Prec(v).Any();
    }

    /// <summary>
    /// Adds edges between the Vertex<T> v and all vertices in the array ws
    /// </summary>
    /// <param name="v"></param>
    /// <param name="ws"></param>
    public void AddEdges(Vertex<T> v, IEnumerable<Vertex<T>> ws)
    {
        foreach (var w in ws)
            AddEdge(v, w);
    }

    /// <summary>
    /// Adds edges between the Vertex<T> v and all vertices in the array ws
    /// </summary>
    /// <param name="vs"></param>
    /// <param name="w"></param>
    public void AddEdges(IEnumerable<Vertex<T>> vs, Vertex<T> w)
    {
        foreach (var v in vs)
            AddEdge(v, w);
    }

    public IEnumerable<Vertex<T>> TopologicalSort()
    {
        return _topo.Sort(this);
    }

    /// <summary>
    /// Checks whether a Vertex<T> with the key given exists in the graph.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool HasVertex(string key)
    {
        return _symbolTable.ContainsKey(key);
    }
}