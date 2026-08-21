namespace BlazorDatasheet.DataStructures.Graph;

/// <summary>
/// Implements Tarjan's strongly connected components algorithm
/// https://en.wikipedia.org/wiki/Tarjan%27s_strongly_connected_components_algorithm
/// </summary>
/// <typeparam name="T"></typeparam>
public class SccSort<T> where T : Vertex
{
    private static readonly Dictionary<VertexKey, T> NoDependents = new();

    private readonly DependencyGraph<T> _graph;

    /// <summary>Depth index assigned to each visited vertex; also serves as the "visited" set.</summary>
    private readonly Dictionary<VertexKey, int> _indices = new();

    private readonly HashSet<VertexKey> _onStack = new();
    private readonly Stack<T> _stack = new();
    private List<IList<T>> _results = new();

    private Frame[] _frames = new Frame[16];
    private int _depth;
    private int _index;

    private struct Frame
    {
        public T Vertex;
        public VertexKey Key;
        public int Index;
        public int Low;
        public Dictionary<VertexKey, T>.ValueCollection.Enumerator Dependents;
    }

    public SccSort(DependencyGraph<T> graph)
    {
        _graph = graph;
    }

    /// <summary>
    /// Calculates the sort order based on dependencies <paramref name="availableVertices"/>. If <paramref name="availableVertices"/> is null, includes all vertices.
    /// </summary>
    /// <param name="availableVertices"></param>
    /// <returns></returns>
    public IList<IList<T>> Sort(IEnumerable<T>? availableVertices = null)
    {
        _indices.Clear();
        _onStack.Clear();
        _stack.Clear();
        _results.Clear();
        _depth = 0;
        _index = 0;

        var vertices = availableVertices ?? _graph.GetAll();

        foreach (var v in vertices)
        {
            if (!_indices.ContainsKey(v.Key))
                StrongConnect(v);
        }

        // Result of this algo is reverse topological sort of a DAG
        _results.Reverse();

        var results = _results;

        // hand the caller its own list - the next Sort() clears ours.
        _results = new List<IList<T>>();
        return results;
    }

    private void StrongConnect(T root)
    {
        Push(root);

        while (_depth > 0)
        {
            ref var frame = ref _frames[_depth - 1];

            if (frame.Dependents.MoveNext())
            {
                var w = frame.Dependents.Current;
                if (!_indices.TryGetValue(w.Key, out var wIndex))
                {
                    // have not yet visited w - descend into it
                    Push(w);
                }
                else if (_onStack.Contains(w.Key))
                {
                    frame.Low = Math.Min(frame.Low, wIndex);
                }

                continue;
            }

            // every dependent of this vertex has been visited
            var vertex = frame.Vertex;
            var key = frame.Key;
            var low = frame.Low;
            var index = frame.Index;
            _depth--;

            if (_depth > 0)
            {
                // the parent's lowlink absorbs ours, as it would on return from the recursive call
                ref var parent = ref _frames[_depth - 1];
                parent.Low = Math.Min(parent.Low, low);
            }

            if (low == index)
            {
                // start a new strongly connected component
                var group = _stack.Peek().Key.Equals(key)
                    ? new List<T>(1) // by far the common case - a vertex that is not in a cycle
                    : new List<T>();

                T w;
                do
                {
                    w = _stack.Pop();
                    _onStack.Remove(w.Key);
                    group.Add(w);
                } while (!key.Equals(w.Key));

                _results.Add(group);
            }
        }
    }

    private void Push(T v)
    {
        if (_depth == _frames.Length)
            Array.Resize(ref _frames, _frames.Length * 2);

        var key = v.Key;
        _indices[key] = _index;
        _stack.Push(v);
        _onStack.Add(key);

        ref var frame = ref _frames[_depth];
        frame.Vertex = v;
        frame.Key = key;
        frame.Index = _index;
        frame.Low = _index;
        frame.Dependents = (_graph.GetDependentsMap(key) ?? NoDependents).Values.GetEnumerator();

        _index++;
        _depth++;
    }
}
