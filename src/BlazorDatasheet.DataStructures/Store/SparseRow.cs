namespace BlazorDatasheet.DataStructures.Store;

internal class SparseRow<T>
{
    private readonly T _defaultValueIfEmpty;
    private Dictionary<int, T> _data = new();
    private int[]? _sortedKeys;

    public SparseRow(T defaultValueIfEmpty)
    {
        _defaultValueIfEmpty = defaultValueIfEmpty;
    }

    internal static SparseRow<T> FromBulkData(T defaultValueIfEmpty, Dictionary<int, T> data)
    {
        return new SparseRow<T>(defaultValueIfEmpty)
        {
            _data = data
        };
    }

    internal void LoadBulkData(Dictionary<int, T> data)
    {
        foreach (var item in data)
            _data[item.Key] = item.Value;

        InvalidateCache();
    }

    private int[] EnsureSortedKeys()
    {
        if (_sortedKeys == null)
        {
            _sortedKeys = _data.Keys.ToArray();
            Array.Sort(_sortedKeys);
        }

        return _sortedKeys;
    }

    private void InvalidateCache() => _sortedKeys = null;

    public int Count => _data.Count;

    public bool ContainsIndex(int itemIndex) => _data.ContainsKey(itemIndex);

    public T Get(int key)
    {
        return _data.TryGetValue(key, out var value) ? value : _defaultValueIfEmpty;
    }

    public void Set(int itemIndex, T value)
    {
        _data[itemIndex] = value;
        InvalidateCache();
    }

    public bool IsEmpty() => _data.Count == 0;

    /// <summary>
    /// Clears a value from memory but doesn't affect any other positions.
    /// </summary>
    public (int removedItemNo, T? value)? Clear(int itemIndex)
    {
        if (_data.Remove(itemIndex, out var removed))
        {
            InvalidateCache();
            return (itemIndex, removed);
        }

        return null;
    }

    public void InsertAt(int itemIndex, int nItems)
    {
        if (nItems <= 0 || _data.Count == 0)
            return;

        var newData = new Dictionary<int, T>(_data.Count);
        foreach (var kvp in _data)
        {
            var shiftedIndex = kvp.Key >= itemIndex ? kvp.Key + nItems : kvp.Key;
            newData[shiftedIndex] = kvp.Value;
        }

        _data = newData;
        InvalidateCache();
    }

    /// <summary>
    /// Returns the nonempty item numbers between and including items i0 to i1
    /// </summary>
    public List<int> GetNonEmptyItemIndicesBetween(int i0, int i1)
    {
        var keys = EnsureSortedKeys();
        var itemIndices = new List<int>();
        if (keys.Length == 0)
            return itemIndices;

        var startIdx = Array.BinarySearch(keys, i0);
        if (startIdx < 0) startIdx = ~startIdx;

        for (int i = startIdx; i < keys.Length; i++)
        {
            if (keys[i] > i1)
                break;
            itemIndices.Add(keys[i]);
        }

        return itemIndices;
    }

    /// <summary>
    /// Returns the nonempty item index numbers and data between and including indices r0 to r1
    /// </summary>
    public IEnumerable<(int itemIndex, T data)> GetNonEmptyDataBetween(int r0, int r1)
    {
        var keys = EnsureSortedKeys();
        if (keys.Length == 0)
            yield break;

        var startIdx = Array.BinarySearch(keys, r0);
        if (startIdx < 0) startIdx = ~startIdx;

        for (int i = startIdx; i < keys.Length; i++)
        {
            var key = keys[i];
            if (key > r1)
                yield break;
            yield return (key, _data[key]);
        }
    }

    /// <summary>
    /// Returns all non-empty data
    /// </summary>
    public List<(int itemIndex, T data)> GetNonEmptyData()
    {
        return _data.Select(x => (x.Key, x.Value)).ToList();
    }

    /// <summary>
    /// Delete items at index given - removing them if found and decreasing item numbers of all items after.
    /// </summary>
    public List<(int indexDeleted, T value)> DeleteAt(int itemIndex, int nItems)
    {
        var deleted = new List<(int indexDeleted, T)>();
        if (nItems <= 0 || _data.Count == 0)
            return deleted;

        long endIndex = (long)itemIndex + nItems - 1;
        var newData = new Dictionary<int, T>(_data.Count);
        foreach (var kvp in _data)
        {
            if (kvp.Key >= itemIndex && kvp.Key <= endIndex)
                deleted.Add((kvp.Key, kvp.Value));
            else
            {
                var shiftedIndex = kvp.Key > endIndex ? kvp.Key - nItems : kvp.Key;
                newData[shiftedIndex] = kvp.Value;
            }
        }

        _data = newData;
        InvalidateCache();
        return deleted;
    }

    /// <summary>
    /// Returns the next non-empty item index after and including the index given. If none found, returns -1
    /// </summary>
    public int GetNextNonEmptyItemKey(int key, int colDir = 1)
    {
        var keys = EnsureSortedKeys();
        if (keys.Length == 0)
            return -1;

        var dir = Math.Sign(colDir);

        var index = Array.BinarySearch(keys, key);

        if (dir > 0)
        {
            if (index >= 0)
                return keys[index];
            index = ~index;
            if (index >= keys.Length)
                return -1;
            return keys[index];
        }

        // dir < 0
        if (index >= 0)
            return keys[index];

        index = ~index - 1;
        if (index < 0)
            return -1;

        return keys[index];
    }

    /// <summary>
    /// Removes all items in list, between (and including) r0 and r1. Does not affect those around it.
    /// </summary>
    public List<(int itemIndexCleared, T)> ClearBetween(int r0, int r1)
    {
        var keys = EnsureSortedKeys();
        var cleared = new List<(int itemIndexCleared, T val)>();

        if (keys.Length == 0)
            return cleared;

        var startIdx = Array.BinarySearch(keys, r0);
        if (startIdx < 0) startIdx = ~startIdx;

        if (startIdx >= keys.Length)
            return cleared;

        for (int i = startIdx; i < keys.Length; i++)
        {
            var key = keys[i];
            if (key > r1)
                break;
            cleared.Add((key, _data[key]));
            _data.Remove(key);
        }

        if (cleared.Count > 0)
            InvalidateCache();

        return cleared;
    }

    public T[] GetDataBetween(int i0, int i1)
    {
        var res = Enumerable.Repeat(_defaultValueIfEmpty, (i1 - i0 + 1)).ToArray();
        foreach (var p in GetNonEmptyDataBetween(i0, i1))
            res[p.itemIndex - i0] = p.data;
        return res;
    }

    public SparseRow<T> GetSubList(int i0, int i1, bool resetIndicesInNewList)
    {
        var sub = new SparseRow<T>(_defaultValueIfEmpty);
        foreach (var dp in GetNonEmptyDataBetween(i0, i1))
        {
            var newIndex = resetIndicesInNewList ? dp.itemIndex - i0 : dp.itemIndex;
            sub._data[newIndex] = dp.data;
        }

        sub.InvalidateCache();
        return sub;
    }
}
