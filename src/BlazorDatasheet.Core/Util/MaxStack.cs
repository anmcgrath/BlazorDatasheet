namespace BlazorDatasheet.Core.Util;

public class MaxStack<T>
{
    private readonly int _limit;
    private readonly List<T> _listImpl;

    /// <summary>
    /// The maximum number of items the stack holds.
    /// </summary>
    public int Limit => _limit;

    /// <summary>
    /// A stack with a maximum size. Once the limit has been reached, the item at the bottom of the stack is removed.
    /// Probably an inefficient way to create a fixed size stack
    /// </summary>
    /// <param name="limit">The maximum size of the stack</param>
    public MaxStack(int limit)
    {
        _limit = limit;
        _listImpl = new List<T>();
    }

    public void Push(T item)
    {
        if (_listImpl.Count >= _limit)
            _listImpl.RemoveAt(0);
        _listImpl.Add(item);
    }

    public T? Pop()
    {
        if (!_listImpl.Any())
            return default(T);

        var topItem = _listImpl.LastOrDefault();
        _listImpl.RemoveAt(_listImpl.Count - 1);
        return topItem;
    }

    public T? Peek()
    {
        return _listImpl.LastOrDefault();
    }

    /// <summary>
    /// Removes and returns the item at the bottom of the stack, i.e. the oldest one.
    /// </summary>
    public T? RemoveBottom()
    {
        if (_listImpl.Count == 0)
            return default(T);

        var bottom = _listImpl[0];
        _listImpl.RemoveAt(0);
        return bottom;
    }

    public IEnumerable<T> GetAllItems()
    {
        return _listImpl;
    }

    public int Count()
    {
        return _listImpl.Count;
    }

    public void Clear()
    {
        _listImpl.Clear();
    }
}