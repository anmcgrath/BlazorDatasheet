using System.Collections;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

public class SparseMatrixStore<T> : IMatrixDataStore<T>
{
    /// <summary>
    /// Matrix columns, with the key being the column index
    /// </summary>
    private readonly Dictionary<int, SColumn<T>> _columns = new();

    public bool Contains(int row, int col)
    {
        if (!_columns.ContainsKey(col))
            return false;
        return _columns[col].Values.ContainsKey(row);
    }

    public T? Get(int row, int col)
    {
        var colExists = _columns.TryGetValue(col, out var column);
        if (!colExists)
            return default(T);

        return column!.Get(row);
    }

    public MatrixRestoreData<T> Set(int row, int col, T value)
    {
        var colExists = _columns.TryGetValue(col, out var column);
        var restoreData = new MatrixRestoreData<T>();

        if (!colExists)
        {
            restoreData.DataRemoved = new List<(int row, int col, T data)>()
            {
                (row, col, default(T))
            };

            column = new SColumn<T>(col);
            _columns.Add(col, column);
        }
        else
        {
            restoreData.DataRemoved = new List<(int row, int col, T? data)>()
            {
                (row, col, column!.Get(row))
            };
        }

        column!.Set(row, value);
        return restoreData;
    }

    public MatrixRestoreData<T> Clear(int row, int col)
    {
        var colExists = _columns.TryGetValue(col, out var column);
        if (!colExists)
            return new MatrixRestoreData<T>();

        var removed = column!.Clear(row, col);
        if (removed == null)
            return new MatrixRestoreData<T>();

        return new MatrixRestoreData<T>()
        {
            DataRemoved = new List<(int row, int col, T? data)>() { removed.Value }
        };
    }

    public MatrixRestoreData<T> Clear(IEnumerable<CellPosition> positions)
    {
        var cleared = positions.Select(x => Clear(x.row, x.col));
        var clearedData = cleared.SelectMany(x => x.DataRemoved).ToList();
        return new MatrixRestoreData<T>()
        {
            DataRemoved = clearedData
        };
    }

    public MatrixRestoreData<T> Clear(IRegion region)
    {
        var cleared = new List<(int row, int col, T?)>();
        foreach (var kp in _columns)
        {
            if (kp.Key < region.Left || kp.Key > region.Right)
                continue;
            cleared.AddRange(kp.Value.ClearBetween(region.Top, region.Bottom)!);
        }

        return new MatrixRestoreData<T>()
        {
            DataRemoved = cleared
        };
    }

    public MatrixRestoreData<T> Clear(IEnumerable<IRegion> regions)
    {
        var cleared = regions.Select(Clear).SelectMany(x => x.DataRemoved);
        return new MatrixRestoreData<T>()
        {
            DataRemoved = cleared.ToList()
        };
    }

    public void InsertRowAt(int row, int nRows = 1)
    {
        foreach (var column in _columns.Values)
        {
            column.InsertRowAt(row, nRows);
        }
    }

    public void InsertColAt(int col, int nCols)
    {
        var currentColumns = _columns.ToList();
        List<(int colIndex, SColumn<T> column)> columnsToReAdd = new List<(int colIndex, SColumn<T> column)>();
        foreach (var kp in currentColumns)
        {
            if (kp.Key >= col)
            {
                _columns.Remove(kp.Key);
                columnsToReAdd.Add((kp.Key + nCols, kp.Value));
            }
        }

        foreach (var c in columnsToReAdd)
        {
            c.column.ColumnIndex = c.colIndex;
            _columns.Add(c.colIndex, c.column);
        }

        for (int i = 0; i < nCols; i++)
        {
            _columns.Add(col + i, new SColumn<T>(col + i));
        }
    }

    public MatrixRestoreData<T> RemoveColAt(int col, int nCols)
    {
        var deleted = new List<(int row, int col, T? data)>();

        for (int i = 0; i < nCols; i++)
        {
            if (_columns.ContainsKey(col + i))
            {
                deleted.AddRange(_columns[col + i].Values.Select(x => (x.Key, col + i, x.Value))!);
                _columns.Remove(col + i);
            }
        }

        List<(int colIndex, SColumn<T> column)> columnsToReAdd = new List<(int colIndex, SColumn<T> column)>();
        var currentColumns = _columns.ToList();
        foreach (var kp in currentColumns)
        {
            if (kp.Key > col)
            {
                _columns.Remove(kp.Key);
                columnsToReAdd.Add((kp.Key - nCols, kp.Value));
            }
        }

        foreach (var c in columnsToReAdd)
        {
            c.column.ColumnIndex = c.colIndex;
            _columns.Add(c.colIndex, c.column);
        }

        return new MatrixRestoreData<T>()
        {
            DataRemoved = deleted
        };
    }

    public int GetNextNonBlankRow(int col, int row)
    {
        if (!_columns.ContainsKey(col))
            return -1;
        return _columns[col].GetNextNonEmptyRow(row);
    }

    public MatrixRestoreData<T> RemoveRowAt(int row, int nRows)
    {
        var deleted = new List<(int row, int col, T?)>();
        foreach (var column in _columns.Values)
            deleted.AddRange(column.DeleteRowAt(row, nRows)!);
        return new MatrixRestoreData<T>()
        {
            DataRemoved = deleted
        };
    }

    /// <summary>
    /// Get non empty cells that exist in the bounds given
    /// </summary>
    /// <param name="r0">The lower row bound</param>
    /// <param name="r1">The upper row bound</param>
    /// <param name="c0">The lower col bound</param>
    /// <param name="c1">The upper col bound</param>
    /// <returns></returns>
    public IEnumerable<CellPosition> GetNonEmptyPositions(int r0, int r1, int c0, int c1)
    {
        List<CellPosition> nonEmptyPositions = new List<CellPosition>();
        foreach (var kp in _columns)
        {
            if (kp.Key < c0 || kp.Key > c1)
                continue;
            var nonEmptyInRow = kp.Value.GetNonEmptyCellsBetween(r0, r1);
            nonEmptyPositions.AddRange(nonEmptyInRow.Select(x => new CellPosition(x, kp.Key)));
        }

        return nonEmptyPositions;
    }

    public IEnumerable<CellPosition> GetNonEmptyPositions(IRegion region)
    {
        return GetNonEmptyPositions(region.Top, region.Bottom, region.Left, region.Right);
    }

    public void Restore(MatrixRestoreData<T> restoreData)
    {
        foreach (var pt in restoreData.DataRemoved)
            Set(pt.row, pt.col, pt.data);
    }

    private class SColumn<T>
    {
        /// <summary>
        /// The values stored in the column. Keys = row Index
        /// </summary>
        public SortedList<int, T> Values { get; set; } = new();

        public int ColumnIndex { get; set; }

        public SColumn(int colIndex)
        {
            ColumnIndex = colIndex;
        }

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

        /// <summary>
        /// Clears a value from memory but doesn't affect any other positions.
        /// </summary>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns>The value that was cleared</return>
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
    }
}