using BlazorDatasheet.FormulaEngine.Graph;

public class TopologicalSort
{
    private Stack<Vertex> _order;
    private HashSet<string> _marked;

    public IEnumerable<Vertex> Sort(DependencyGraph g)
    {
        _order = new Stack<Vertex>();
        _marked = new();
        
        // TODO graph must be DAG so handle cycles
        
        foreach (var v in g.GetAll())
            if (!Marked(v))
                dfs(g, v);

        return _order;
    }

    private bool Marked(Vertex v)
    {
        return _marked.Contains(v.Key);
    }

    private void dfs(DependencyGraph g, Vertex v)
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