namespace BlazorDatasheet.DataStructures.Graph;

public class TopologicalSort<T> where T:Vertex
{
    private Stack<T> _order;
    private HashSet<string> _marked;

    public IEnumerable<T> Sort(DependencyGraph<T> g)
    {
        _order = new Stack<T>();
        _marked = new();

        // TODO graph must be DAG so handle cycles

        foreach (var v in g.GetAll())
            if (!Marked(v))
                dfs(g, v);

        return _order;
    }

    private bool Marked(T v)
    {
        return _marked.Contains(v.Key);
    }

    private void dfs(DependencyGraph<T> g, T v)
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