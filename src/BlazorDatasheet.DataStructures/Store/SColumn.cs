using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

internal class SColumn<T>
{
    private readonly T _defaultValueIfEmpty;

    /// <summary>
    /// The values stored in the column. Keys = row Index
    /// </summary>
    public SortedList<int, T> Values { get; set; } = new();

    public int ColumnIndex { get; set; }

    public SColumn(int colIndex, T defaultValueIfEmpty)
    {
        _defaultValueIfEmpty = defaultValueIfEmpty;
        ColumnIndex = colIndex;
    }

    public T Get(int row)
    {
        if (Values.TryGetValue(row, out var value))
            return value;
        return _defaultValueIfEmpty;
    }

    public void Set(int row, T value)
    {
        if (!Values.ContainsKey(row))
            Values.Add(row, value);
        else
            Values[row] = value;
    }

    /// <summary>
    /// Clears a value from memory but doesn't affect any other positions.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns>The value that was cleared</returns>
    public (int row, int col, T?)? Clear(int row, int col)
    {
        var index = Values.IndexOfKey(row);
        if (index >= 0)
        {
            var removed = Values[row];
            Values.Remove(row);
            return (row, ColumnIndex, removed);
        }

        return null;
    }

    public void InsertRowAt(int row, int nRows)
    {
        // Find where the next row should be inserted at in the dict
        var index = Values.Keys.BinarySearchIndexOf(row - 1, Comparer<int>.Default);
        if (index < 0)
            index = ~index;
        else
            index++; // this is the next index after the value

        if (index < 0 || index >= Values.Count)
            return;

        var nValues = Values.Keys.Count;
        // Work backwards from the end of the data to where we have
        // inserted the row and increase the row values by 1
        // (when we insert a row we don't add any new rows to Values)
        for (int i = nValues - 1; i >= index; i--)
        {
            // Shuffle up the values
            var val = Values.Values[i];
            var newRowNum = Values.Keys[i] + nRows;
            Values.RemoveAt(i);
            Values.Add(newRowNum, val);
        }
    }

    /// <summary>
    /// Returns the nonempty row numbers between & including rows r0 to r1
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns></returns>
    public List<int> GetNonEmptyRowsBetween(int r0, int r1)
    {
        var rows = new List<int>();
        if (!Values.Any())
            return rows;
        var indexStart = Values.Keys.BinarySearchClosest(r0);
        var index = indexStart;

        for (int i = index; i < Values.Keys.Count; i++)
        {
            var rowAtI = Values.Keys[i];
            if (rowAtI < r0 || rowAtI > r1)
                break;
            rows.Add(rowAtI);
        }

        return rows;
    }

    /// <summary>
    /// Returns the nonempty row numbers between & including rows r0 to r1
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns></returns>
    public List<(int row, T data)> GetNonEmptyDataBetween(int r0, int r1)
    {
        var rows = new List<(int row, T data)>();
        if (!Values.Any())
            return rows;

        var indexStart = Values.Keys.BinarySearchClosest(r0);
        var index = indexStart;

        for (int i = index; i < Values.Keys.Count; i++)
        {
            var rowAtI = Values.Keys[i];
            if (rowAtI < r0 || rowAtI > r1)
                break;
            rows.Add((rowAtI, Values[rowAtI]));
        }

        return rows;
    }

    /// <summary>
    /// "Delete a row" - deleting it if it is found but regardless decreasing the row numbers of all rows after it.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="nRows"></param>
    /// <returns>The removed values</returns>
    public IEnumerable<(int row, int col, T)> DeleteRowAt(int row, int nRows)
    {
        var deleted = new List<(int row, int col, T)>();

        // Find where the next row should be inserted after in the dict
        var startIndex = Values.Keys.BinarySearchIndexOf(row, Comparer<int>.Default);
        if (startIndex < 0)
            startIndex = ~startIndex; // the index points to the next row 

        if (startIndex > Values.Count - 1)
            return new List<(int row, int col, T)>();

        int startRow = Values.Keys[startIndex];
        if (startRow < row)
            startIndex++;

        var endIndex = Values.Keys.BinarySearchClosest(row + nRows - 1);
        endIndex = Math.Min(endIndex, Values.Count - 1);

        var endRow = Values.Keys[endIndex];
        if (endRow > row + nRows - 1)
            endIndex--;

        for (int i = 0; i <= (endIndex - startIndex); i++)
        {
            deleted.Add((Values.GetKeyAtIndex(startIndex), ColumnIndex, Values.GetValueAtIndex(startIndex)));
            Values.RemoveAt(startIndex);
        }

        for (int i = startIndex; i < Values.Count; i++)
        {
            // Shuffle down the values
            var val = Values.Values[i];
            var newRowNum = Values.Keys[i] - nRows;
            Values.RemoveAt(i);
            Values.Add(newRowNum, val);
        }

        return deleted;
    }

    public int GetNextNonEmptyRow(int row)
    {
        var index = Values.Keys.BinarySearchIndexOf(row, Comparer<int>.Default);
        if (index < 0)
            index = ~index;

        index++;
        if (index >= Values.Keys.Count)
            return -1;
        return Values.Keys[index];
    }

    /// <summary>
    /// Removes all rows in column, between (and including) r0 and r1
    /// </summary>
    /// <param name="r0"></param>
    /// <param name="r1"></param>
    /// <returns></returns>
    public IEnumerable<(int row, int col, T)> ClearBetween(int r0, int r1)
    {
        var cleared = new List<(int row, int col, T val)>();
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
            cleared.Add((Values.GetKeyAtIndex(startIndex), ColumnIndex, Values.GetValueAtIndex(startIndex)));
            Values.RemoveAt(startIndex);
            nRemoved++;
        }

        return cleared;
    }

    public bool ContainsRow(int row)
    {
        return Values.ContainsKey(row);
    }
}