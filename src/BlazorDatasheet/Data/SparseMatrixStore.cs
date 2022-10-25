using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

public class SparseMatrixStore<T> : IMatrixDataStore<T>
{
    private Dictionary<int, SColumn<T>> Columns { get; set; } = new();

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

    public void InsertRowAt(int row)
    {
        foreach (var column in Columns.Values)
        {
            column.InsertRowAt(row);
        }
    }

    public int GetNextNonEmptyRow(int col, int row)
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

    private class SColumn<T>
    {
        /// <summary>
        /// The values stored in the column. Keys = row Index
        /// </summary>
        private SortedList<int, T> Values { get; set; } = new();

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