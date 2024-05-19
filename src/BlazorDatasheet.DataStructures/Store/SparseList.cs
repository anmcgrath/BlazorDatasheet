using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

internal class SparseList<T>
{
    private readonly T _defaultValueIfEmpty;

    /// <summary>
    /// The values stored in the list. Keys = Item Index
    /// </summary>
    public SortedList<int, T> Values { get; set; } = new();

    public SparseList(T defaultValueIfEmpty)
    {
        _defaultValueIfEmpty = defaultValueIfEmpty;
    }

    public bool ContainsIndex(int itemIndex)
    {
        return Values.ContainsKey(itemIndex);
    }

    public T Get(int key)
    {
        return Values.GetValueOrDefault(key, _defaultValueIfEmpty);
    }

    public void Set(int itemIndex, T value)
    {
        Values[itemIndex] = value;
    }

    /// <summary>
    /// Clears a value from memory but doesn't affect any other positions.
    /// </summary>
    /// <param name="itemIndex"></param>
    /// <returns>The value that was cleared</returns>
    public (int removedItemNo, T? value)? Clear(int itemIndex)
    {
        var index = Values.IndexOfKey(itemIndex);
        if (index >= 0)
        {
            var removed = Values[itemIndex];
            Values.Remove(itemIndex);
            return (itemIndex, removed);
        }

        return null;
    }

    public void InsertAt(int itemIndex, int nItems)
    {
        // Find where the next col should be inserted at in the dict
        var index = Values.Keys.BinarySearchIndexOf(itemIndex - 1, Comparer<int>.Default);
        if (index < 0)
            index = ~index;
        else
            index++; // this is the next index after the value

        if (index < 0 || index >= Values.Count)
            return;

        var nValues = Values.Keys.Count;
        // Work backwards from the end of the data to where we have
        // inserted the col and increase the col values by 1
        // (when we insert a col we don't add any new cols to Values)
        for (int i = nValues - 1; i >= index; i--)
        {
            // Shuffle up the values
            var val = Values.Values[i];
            var newIndexNum = Values.Keys[i] + nItems;
            Values.RemoveAt(i);
            Values.Add(newIndexNum, val);
        }
    }

    /// <summary>
    /// Returns the nonempty item numbers between & including items r0 to r1
    /// </summary>
    /// <param name="i0"></param>
    /// <param name="i1"></param>
    /// <returns></returns>
    public List<int> GotNonEmptyItemIndicesBetween(int i0, int i1)
    {
        var itemIndices = new List<int>();
        if (!Values.Any())
            return itemIndices;
        var indexStart = Values.Keys.BinarySearchClosest(i0);
        var index = indexStart;

        for (int i = index; i < Values.Keys.Count; i++)
        {
            var itemIndexAti = Values.Keys[i];
            if (itemIndexAti < i0 || itemIndexAti > i1)
                break;
            itemIndices.Add(itemIndexAti);
        }

        return itemIndices;
    }

    /// <summary>
    /// Returns the nonempty item index numbers & data between & including indices r0 to r1
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns></returns>
    public List<(int itemIndex, T data)> GetNonEmptyDataBetween(int r0, int r1)
    {
        if (!Values.Any())
            return new List<(int itemIndex, T data)>();

        var items = new List<(int itemIndex, T data)>();

        var indexStart = Values.Keys.BinarySearchClosest(r0);
        var index = indexStart;

        for (int i = index; i < Values.Keys.Count; i++)
        {
            var itemIndexAti = Values.Keys[i];
            if (itemIndexAti < r0 || itemIndexAti > r1)
                break;
            items.Add((itemIndexAti, Values[itemIndexAti]));
        }

        return items;
    }

    /// <summary>
    /// Returns all non-empty data
    /// </summary>
    /// <returns></returns>
    public List<(int itemIndex, T data)> GetNonEmptyData()
    {
        return Values.Select(x => (x.Key, x.Value)).ToList();
    }

    /// <summary>
    /// "Delete an item at index given" - deleting it if it is found but regardless decreasing the item numbers of all rows after it.
    /// </summary>
    /// <param name="itemIndex"></param>
    /// <param name="nItems"></param>
    /// <returns>The removed values</returns>
    public List<(int indexDeleted, T value)> DeleteAt(int itemIndex, int nItems)
    {
        var deleted = new List<(int indexDeleted, T)>();

        // Find where the next row should be inserted after in the dict
        var startIndex = Values.Keys.BinarySearchIndexOf(itemIndex, Comparer<int>.Default);
        if (startIndex < 0)
            startIndex = ~startIndex; // the index points to the next row 

        if (startIndex > Values.Count - 1)
            return new List<(int indexDeleted, T)>();

        int startItemIndex = Values.Keys[startIndex];
        if (startItemIndex < itemIndex)
            startIndex++;

        var endIndex = Values.Keys.BinarySearchClosest(itemIndex + nItems - 1);
        endIndex = Math.Min(endIndex, Values.Count - 1);

        var endItemIndex = Values.Keys[endIndex];
        if (endItemIndex > itemIndex + nItems - 1)
            endIndex--;

        for (int i = 0; i <= (endIndex - startIndex); i++)
        {
            deleted.Add((Values.GetKeyAtIndex(startIndex), Values.GetValueAtIndex(startIndex)));
            Values.RemoveAt(startIndex);
        }

        for (int i = startIndex; i < Values.Count; i++)
        {
            // Shuffle down the values
            var val = Values.Values[i];
            var newItemIndex = Values.Keys[i] - nItems;
            Values.RemoveAt(i);
            Values.Add(newItemIndex, val);
        }

        return deleted;
    }

    /// <summary>
    /// Returns the next non-empty item index after & including the index given. If none found, returns -1
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public int GetNextNonEmptyItemKey(int key)
    {
        if (!Values.Any())
            return -1;

        var index = Values.Keys.BinarySearchIndexOf(key, Comparer<int>.Default);
        if (index < 0)
            index = ~index;

        if (index >= Values.Keys.Count)
            return -1;

        return Values.Keys[index];
    }

    /// <summary>
    /// Returns the next non-empty value pair after & including the index given. If none found, returns null
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public (int Key, T Value)? GetNextNonEmptyItemValuePair(int key)
    {
        if (!Values.Any())
            return null;

        var index = Values.Keys.BinarySearchIndexOf(key, Comparer<int>.Default);
        if (index < 0)
            index = ~index;

        if (index >= Values.Keys.Count)
            return null;

        return (Values.Keys[index], Values.Values[index]);
    }

    /// <summary>
    /// Removes all items in list, between (and including) r0 and r1. Does not affect those around it.
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns>The cleared items</returns>
    public List<(int itemIndexCleared, T)> ClearBetween(int r0, int r1)
    {
        var cleared = new List<(int itemIndexCleared, T val)>();
        var startIndex = Values.Keys.BinarySearchIndexOf(r0);

        if (startIndex < 0)
            startIndex = ~startIndex;

        if (startIndex > Values.Count - 1) // we have no values, so nothing to clear
            return cleared;

        // start index is now either the actual start or the index after where the row would be found
        var endIndex = Values.Keys.BinarySearchIndexOf(r1);
        if (endIndex < 0)
            endIndex = ~endIndex - 1;

        if (endIndex < startIndex)
            return cleared;

        var n = (endIndex - startIndex + 1);
        var nRemoved = 0;
        while (nRemoved < n)
        {
            cleared.Add((Values.GetKeyAtIndex(startIndex), Values.GetValueAtIndex(startIndex)));
            Values.RemoveAt(startIndex);
            nRemoved++;
        }

        return cleared;
    }

    public T[] GetDataBetween(int i0, int i1)
    {
        var res = Enumerable.Repeat(_defaultValueIfEmpty, (i1 - i0 + 1)).ToArray();
        var nonEmpty = GetNonEmptyDataBetween(i0, i1);
        foreach (var p in nonEmpty)
            res[p.itemIndex - i0] = p.data;
        return res;
    }

    public SparseList<T> GetSubList(int i0, int i1, bool resetIndicesInNewList)
    {
        var vals = new SortedList<int, T>();
        var nonEmpty = GetNonEmptyDataBetween(i0, i1);

        foreach (var dp in nonEmpty)
        {
            var newIndex = resetIndicesInNewList ? dp.itemIndex - i0 : dp.itemIndex;
            vals.Add(newIndex, dp.data);
        }

        return new SparseList<T>(_defaultValueIfEmpty)
        {
            Values = vals
        };
    }
}