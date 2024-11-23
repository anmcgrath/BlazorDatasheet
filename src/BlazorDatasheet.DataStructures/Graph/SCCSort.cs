namespace BlazorDatasheet.DataStructures.Graph;

/// <summary>
/// Implements Tarjan's strongly connected components algorithm
/// https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm
/// </summary>
/// <typeparam name="T"></typeparam>
public class SccSort<T> where T : Vertex
{
    private readonly DependencyGraph<T> _graph;
    private Dictionary<string, int> _indices = null!;
    private Dictionary<string, int> _low = null!;
    private List<IList<T>> _results = null!;
    private Stack<T> _stack = null!;
    private int _index;

    public SccSort(DependencyGraph<T> graph)
    {
        _graph = graph;
    }

    public IList<IList<T>> Sort()
    {
        _indices = new();
        _low = new();
        _results = new();
        _stack = new();
        _index = 0;
        
        foreach (var v in _graph.GetAll())
        {
            if (!_indices.ContainsKey(v.Key))
                StrongConnect(v);
        }

        // Result of this algo is reverse topological sort of a DAG
        _results.Reverse();

        return _results;
    }

    private void StrongConnect(T v)
    {
        // set depth index for v to smallest unused index
        _indices[v.Key] = _index;
        _low[v.Key] = _index;
        _index++;
        _stack.Push(v);

        foreach (var w in _graph.Adj(v))
        {
            if (!_indices.ContainsKey(w.Key))
            {
                // have not yet visited w
                StrongConnect(w);
                _low[v.Key] = Math.Min(_low[v.Key], _low[w.Key]);
            }
            else if (_stack.Contains(w))
                _low[v.Key] = Math.Min(_low[v.Key], _indices[w.Key]);
        }

        if (_low[v.Key] == _indices[v.Key])
        {
            // start a new strongly connected component
            var g = new List<T>();
            T w;
            do
            {
                w = _stack.Pop();
                g.Add(w);
            } while (v.Key != w.Key);

            _results.Add(g);
        }
    }
}