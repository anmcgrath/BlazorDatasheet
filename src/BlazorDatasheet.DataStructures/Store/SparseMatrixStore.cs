using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.Data;

public class SparseMatrixStore<T> : IMatrixDataStore<T>
{
    /// <summary>
    /// Matrix columns, with the key being the column index
    /// </summary>
    private Dictionary<int, SColumn<T>> Columns { get; set; } = new();

    public bool Contains(int row, int col)
    {
        if (!Columns.ContainsKey(col))
            return false;
        return Columns[col].Values.ContainsKey(row);
    }

    public T? Get(int row, int col)
    {
        var colExists = Columns.TryGetValue(col, out var column);
        if (!colExists)
            return default(T);

        return column.Get(row);
    }

    public void Set(int row, int col, T value)
    {
        var colExists = Columns.TryGetValue(col, out var column);
        if (!colExists)
        {
            column = new SColumn<T>();
            Columns.Add(col, column);
        }

        column.Set(row, value);
    }

    public void InsertRowAfter(int row)
    {
        foreach (var column in Columns.Values)
        {
            column.InsertRowAt(row);
        }
    }

    public void InsertColAfter(int col)
    {
        var currentColumns = Columns.ToList();
        List<(int col, SColumn<T> column)> columnsToAdd = new List<(int col, SColumn<T> column)>();
        foreach (var kp in currentColumns)
        {
            if (kp.Key > col)
            {
                Columns.Remove(kp.Key);
                columnsToAdd.Add((kp.Key + 1, kp.Value));
            }
        }

        foreach (var c in columnsToAdd)
            Columns.Add(c.col, c.column);

        Columns.Add(col + 1, new SColumn<T>());
    }

    public void RemoveColAt(int col)
    {
        if (Columns.ContainsKey(col))
            Columns.Remove(col);
        List<(int col, SColumn<T> column)> columnsToAdd = new List<(int col, SColumn<T> column)>();
        var currentColumns = Columns.ToList();
        foreach (var kp in currentColumns)
        {
            if (kp.Key > col)
            {
                Columns.Remove(kp.Key);
                columnsToAdd.Add((kp.Key - 1, kp.Value));
            }
        }

        foreach (var c in columnsToAdd)
            Columns.Add(c.col, c.column);
    }

    public int GetNextNonBlankRow(int col, int row)
    {
        if (!Columns.ContainsKey(col))
            return -1;
        return Columns[col].GetNextNonEmptyRow(row);
    }

    public void RemoveRowAt(int row)
    {
        foreach (var column in Columns.Values)
        {
            column.DeleteRowAt(row);
        }
    }

    /// <summary>
    /// Get non empty cells that exist in the bounds given
    /// </summary>
    /// <param name="r0">The lower row bound</param>
    /// <param name="r1">The upper row bound</param>
    /// <param name="c0">The lower col bound</param>
    /// <param name="c1">The upper col bound</param>
    /// <returns></returns>
    public IEnumerable<(int row, int col)> GetNonEmptyPositions(int r0, int r1, int c0, int c1)
    {
        List<(int row, int col)> nonEmptyPositions = new List<(int row, int col)>();
        foreach (var kp in Columns)
        {
            if (kp.Key < c0 || kp.Key > c1)
                continue;
            var nonEmptyInRow = kp.Value.GetNonEmptyCellsBetween(r0, r1);
            nonEmptyPositions.AddRange(nonEmptyInRow.Select(x => (x, kp.Key)));
        }

        return nonEmptyPositions;
    }

    private class SColumn<T>
    {
        /// <summary>
        /// The values stored in the column. Keys = row Index
        /// </summary>
        public SortedList<int, T> Values { get; set; } = new();

        public T? Get(int row)
        {
            if (Values.TryGetValue(row, out var value))
                return value;
            return default(T);
        }

        public void Set(int row, T value)
        {
            if (!Values.ContainsKey(row))
                Values.Add(row, value);
            else
                Values[row] = value;
        }

        public void InsertRowAt(int row)
        {
            // Find where the next row should be inserted after in the dict
            var index = Values.Keys.BinarySearchIndexOf(row, Comparer<int>.Default);
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
                var newRowNum = Values.Keys[i] + 1;
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
        public List<int> GetNonEmptyCellsBetween(int r0, int r1)
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
        /// "Delete a row" - deleting it if it is found but regardless decreasing the row numbers of all rows after it.
        /// </summary>
        /// <param name="row"></param>
        public void DeleteRowAt(int row)
        {
            // Find where the next row should be inserted after in the dict
            var index = Values.Keys.BinarySearchIndexOf(row, Comparer<int>.Default);
            if (index < 0)
                index = ~index;
            else
            {
                Values.RemoveAt(index);
                // Since we've removed we don't have to ++ index because
                // it is now at the next highest row.
            }

            if (index < 0 || index >= Values.Count)
                return;

            for (int i = index; i < Values.Count; i++)
            {
                // Shuffle down the values
                var val = Values.Values[i];
                var newRowNum = Values.Keys[i] - 1;
                Values.RemoveAt(i);
                Values.Add(newRowNum, val);
            }
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
    }
}