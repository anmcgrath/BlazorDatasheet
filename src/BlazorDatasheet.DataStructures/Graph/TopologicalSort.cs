namespace BlazorDatasheet.DataStructures.Graph;

public class TopologicalSort<T>
{
    private Stack<Vertex<T>> _order;
    private HashSet<string> _marked;

    public IEnumerable<Vertex<T>> Sort(DependencyGraph<T> g)
    {
        _order = new Stack<Vertex<T>>();
        _marked = new();

        // TODO graph must be DAG so handle cycles

        foreach (var v in g.GetAll())
            if (!Marked(v))
                dfs(g, v);

        return _order;
    }

    private bool Marked(Vertex<T> v)
    {
        return _marked.Contains(v.Key);
    }

    private void dfs(DependencyGraph<T> g, Vertex<T> v)
    {
        _marked.Add(v.Key);
        foreach (var w in g.Adj(v))
        {
            if (!Marked(w))
                dfs(g, w);
        }

        _order.Push(v);
    }
}