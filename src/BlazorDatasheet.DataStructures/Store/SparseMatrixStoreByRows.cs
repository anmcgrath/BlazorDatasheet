using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public class SparseMatrixStoreByRows<T> : IMatrixDataStore<T>
{
    private Dictionary<int, SparseRow<T>> _rows = new();
    private int[]? _sortedRowKeys;
    private readonly T? _defaultIfEmpty;
    private readonly EqualityComparer<T> _valueComparer = EqualityComparer<T>.Default;

    public SparseMatrixStoreByRows(T defaultIfEmpty)
    {
        _defaultIfEmpty = defaultIfEmpty;
    }

    public SparseMatrixStoreByRows() : this(default(T))
    {
    }

    public void BulkLoad(T[][] values, int rowOffset = 0, int colOffset = 0)
    {
        var rows = new Dictionary<int, SparseRow<T>>(values.Length);
        for (int rowIndex = 0; rowIndex < values.Length; rowIndex++)
        {
            var rowValues = values[rowIndex];
            if (rowValues.Length == 0)
                continue;

            var rowData = BuildRowData(rowValues, colOffset);
            if (rowData == null)
                continue;

            rows[rowIndex + rowOffset] = rowData;
        }

        _rows = rows;
        _sortedRowKeys = null;
    }

    public void BulkLoad(T[,] values, int rowOffset = 0, int colOffset = 0)
    {
        int rowCount = values.GetLength(0);
        int colCount = values.GetLength(1);
        var rows = new Dictionary<int, SparseRow<T>>(rowCount);

        for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            var rowData = BuildRowData(values, rowIndex, colCount, colOffset);
            if (rowData == null)
                continue;

            rows[rowIndex + rowOffset] = rowData;
        }

        _rows = rows;
        _sortedRowKeys = null;
    }

    private int[] EnsureSortedRowKeys()
    {
        if (_sortedRowKeys == null)
        {
            _sortedRowKeys = _rows.Keys.ToArray();
            Array.Sort(_sortedRowKeys);
        }

        return _sortedRowKeys;
    }

    private void InvalidateRowCache() => _sortedRowKeys = null;

    private SparseRow<T>? BuildRowData(T[] rowValues, int colOffset)
    {
        int nonDefaultCount = 0;
        for (int colIndex = 0; colIndex < rowValues.Length; colIndex++)
        {
            if (!_valueComparer.Equals(rowValues[colIndex], _defaultIfEmpty!))
                nonDefaultCount++;
        }

        if (nonDefaultCount == 0)
            return null;

        var data = new Dictionary<int, T>(nonDefaultCount);
        for (int colIndex = 0; colIndex < rowValues.Length; colIndex++)
        {
            var value = rowValues[colIndex];
            if (_valueComparer.Equals(value, _defaultIfEmpty!))
                continue;

            data[colIndex + colOffset] = value;
        }

        return SparseRow<T>.FromBulkData(_defaultIfEmpty!, data);
    }

    private SparseRow<T>? BuildRowData(T[,] values, int rowIndex, int colCount, int colOffset)
    {
        int nonDefaultCount = 0;
        for (int colIndex = 0; colIndex < colCount; colIndex++)
        {
            if (!_valueComparer.Equals(values[rowIndex, colIndex], _defaultIfEmpty!))
                nonDefaultCount++;
        }

        if (nonDefaultCount == 0)
            return null;

        var data = new Dictionary<int, T>(nonDefaultCount);
        for (int colIndex = 0; colIndex < colCount; colIndex++)
        {
            var value = values[rowIndex, colIndex];
            if (_valueComparer.Equals(value, _defaultIfEmpty!))
                continue;

            data[colIndex + colOffset] = value;
        }

        return SparseRow<T>.FromBulkData(_defaultIfEmpty!, data);
    }

    public bool Contains(int row, int col)
    {
        return _rows.TryGetValue(row, out var rowData) && rowData.ContainsIndex(col);
    }

    public T Get(int row, int col)
    {
        if (_rows.TryGetValue(row, out var rowData))
            return rowData.Get(col);
        return _defaultIfEmpty!;
    }

    public MatrixRestoreData<T> Set(int row, int col, T value)
    {
        var restoreData = new MatrixRestoreData<T>();
        if (!_rows.TryGetValue(row, out var rowData))
        {
            rowData = new SparseRow<T>(_defaultIfEmpty!);
            _rows[row] = rowData;
            InvalidateRowCache();
        }

        var currValue = rowData.Get(col);
        rowData.Set(col, value);
        restoreData.DataRemoved = new() { (row, col, currValue) };
        return restoreData;
    }

    public MatrixRestoreData<T> Clear(int row, int col)
    {
        if (!_rows.TryGetValue(row, out var rowData))
            return new MatrixRestoreData<T>();

        var restoreData = rowData.Clear(col);

        if (rowData.IsEmpty())
        {
            _rows.Remove(row);
            InvalidateRowCache();
        }

        if (restoreData == null)
            return new MatrixRestoreData<T>();

        return new MatrixRestoreData<T>()
        {
            DataRemoved = new() { (row, col, restoreData.Value.value) }
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
        var cleared = new List<(int row, int col, T)>();
        var keys = EnsureSortedRowKeys();

        var startIdx = Array.BinarySearch(keys, region.Top);
        if (startIdx < 0) startIdx = ~startIdx;

        var rowsToRemove = new List<int>();
        for (int i = startIdx; i < keys.Length; i++)
        {
            var rowKey = keys[i];
            if (rowKey > region.Bottom)
                break;

            var rowData = _rows[rowKey];
            var clearedRowData = rowData.ClearBetween(region.Left, region.Right);
            cleared.AddRange(clearedRowData.Select(x =>
                (rowKey, x.itemIndexCleared, x.Item2)
            ));

            if (rowData.IsEmpty())
                rowsToRemove.Add(rowKey);
        }

        foreach (var rowKey in rowsToRemove)
            _rows.Remove(rowKey);

        if (rowsToRemove.Count > 0)
            InvalidateRowCache();

        return new MatrixRestoreData<T>()
        {
            DataRemoved = cleared!
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

    public MatrixRestoreData<T> InsertRowAt(int row, int count)
    {
        var newRows = new Dictionary<int, SparseRow<T>>(_rows.Count);
        foreach (var kvp in _rows)
        {
            var newKey = kvp.Key >= row ? kvp.Key + count : kvp.Key;
            newRows[newKey] = kvp.Value;
        }

        _rows = newRows;
        InvalidateRowCache();
        return new MatrixRestoreData<T>()
        {
            Shifts = [new AppliedShift(Axis.Row, row, count, null)]
        };
    }

    public MatrixRestoreData<T> InsertColAt(int col, int count)
    {
        foreach (var row in _rows.Values)
            row.InsertAt(col, count);

        return new MatrixRestoreData<T>()
        {
            Shifts = [new AppliedShift(Axis.Col, col, count, null)]
        };
    }

    public MatrixRestoreData<T> RemoveColAt(int col, int count)
    {
        var removed = new List<(int row, int col, T data)>();
        foreach (var kvp in _rows)
        {
            var removedColData = kvp.Value.DeleteAt(col, count);
            removed.AddRange(removedColData.Select(x =>
                (kvp.Key, x.indexDeleted, x.value)
            ));
        }

        return new MatrixRestoreData<T>()
        {
            DataRemoved = removed!,
            Shifts = [new AppliedShift(Axis.Col, col, -count, null)]
        };
    }

    public int GetNextNonBlankRow(int row, int col)
    {
        var keys = EnsureSortedRowKeys();
        var startIdx = Array.BinarySearch(keys, row + 1);
        if (startIdx < 0) startIdx = ~startIdx;

        for (int i = startIdx; i < keys.Length; i++)
        {
            if (_rows[keys[i]].ContainsIndex(col))
                return keys[i];
        }

        return -1;
    }

    public int GetNextNonEmptyIndex(int index)
    {
        var keys = EnsureSortedRowKeys();
        var startIdx = Array.BinarySearch(keys, index + 1);
        if (startIdx < 0) startIdx = ~startIdx;

        if (startIdx >= keys.Length)
            return -1;

        return keys[startIdx];
    }

    public int GetNextNonEmptyIndexInRow(int row, int col, int colDirection = 1)
    {
        if (!_rows.TryGetValue(row, out var rowData))
            return -1;

        return rowData.GetNextNonEmptyItemKey(col + Math.Sign(colDirection), colDirection);
    }

    public MatrixRestoreData<T> RemoveRowAt(int row, int count)
    {
        long endIndex = (long)row + count - 1;
        var removedData = new List<(int row, int col, T data)>();
        var newRows = new Dictionary<int, SparseRow<T>>(_rows.Count);

        foreach (var kvp in _rows)
        {
            if (kvp.Key >= row && kvp.Key <= endIndex)
            {
                var nonEmptyColData = kvp.Value.GetNonEmptyData();
                removedData.AddRange(nonEmptyColData.Select(x =>
                    (kvp.Key, x.itemIndex, x.data)
                ));
            }
            else
            {
                var newKey = kvp.Key > endIndex ? kvp.Key - count : kvp.Key;
                newRows[newKey] = kvp.Value;
            }
        }

        _rows = newRows;
        InvalidateRowCache();

        return new MatrixRestoreData<T>()
        {
            DataRemoved = removedData!,
            Shifts = [new AppliedShift(Axis.Row, row, -count, null)]
        };
    }

    public IEnumerable<CellPosition> GetNonEmptyPositions(int r0, int r1, int c0, int c1)
    {
        var keys = EnsureSortedRowKeys();
        var startIdx = Array.BinarySearch(keys, r0);
        if (startIdx < 0) startIdx = ~startIdx;

        for (int i = startIdx; i < keys.Length; i++)
        {
            var rowKey = keys[i];
            if (rowKey > r1)
                yield break;

            foreach (var col in _rows[rowKey].GetNonEmptyDataBetween(c0, c1))
                yield return new CellPosition(rowKey, col.itemIndex);
        }
    }

    public MatrixRestoreData<T> Copy(IRegion fromRegion, IRegion toRegion)
    {
        var nonEmptyCopyData = this.GetNonEmptyData(fromRegion).ToList();
        var restoreData = this.Clear(toRegion);

        var dr = toRegion.Top - fromRegion.Top;
        var dc = toRegion.Left - fromRegion.Left;

        foreach (var v in nonEmptyCopyData)
            this.Set(v.row + dr, v.col + dc, v.data);

        return restoreData;
    }

    public void Restore(MatrixRestoreData<T> restoreData)
    {
        if (restoreData.Shifts != null)
        {
            foreach (var shift in restoreData.Shifts)
            {
                if (shift.Amount > 0)
                {
                    if (shift.Axis == Axis.Col)
                        RemoveColAt(shift.Index, shift.Amount);
                    else
                        RemoveRowAt(shift.Index, shift.Amount);
                }
                else
                {
                    if (shift.Axis == Axis.Col)
                        InsertColAt(shift.Index, -shift.Amount);
                    else
                        InsertRowAt(shift.Index, -shift.Amount);
                }
            }
        }

        foreach (var pt in restoreData.DataRemoved)
        {
            if (pt.data?.Equals(_defaultIfEmpty) == true)
                Clear(pt.row, pt.col);
            else
                Set(pt.row, pt.col, pt.data!);
        }
    }

    public IEnumerable<(int row, int col, T data)> GetNonEmptyData(IRegion region)
    {
        var data = new List<(int row, int col, T data)>();
        var keys = EnsureSortedRowKeys();
        var startIdx = Array.BinarySearch(keys, region.Top);
        if (startIdx < 0) startIdx = ~startIdx;

        for (int i = startIdx; i < keys.Length; i++)
        {
            var rowKey = keys[i];
            if (rowKey > region.Bottom)
                break;

            foreach (var col in _rows[rowKey].GetNonEmptyDataBetween(region.Left, region.Right))
                data.Add((rowKey, col.itemIndex, col.data));
        }

        return data;
    }

    public T[][] GetData(IRegion region)
    {
        var res = new T[region.Height][];
        for (int i = 0; i < region.Height; i++)
        {
            if (_rows.TryGetValue(i + region.Top, out var rowData))
                res[i] = rowData.GetDataBetween(region.Left, region.Right);
            else
                res[i] = Enumerable.Repeat(_defaultIfEmpty!, region.Width).ToArray();
        }

        return res;
    }

    public IMatrixDataStore<T> GetSubStore(IRegion region, bool newStoreResetsOffsets = true)
    {
        var store = new SparseMatrixStoreByRows<T>(_defaultIfEmpty);
        int r0 = region.Top;
        int r1 = region.Bottom;
        int c0 = region.Left;
        int c1 = region.Right;

        int rowOffset = newStoreResetsOffsets ? r0 : 0;

        var keys = EnsureSortedRowKeys();
        var startIdx = Array.BinarySearch(keys, r0);
        if (startIdx < 0) startIdx = ~startIdx;

        for (int i = startIdx; i < keys.Length; i++)
        {
            var rowKey = keys[i];
            if (rowKey > r1)
                break;

            var subRow = _rows[rowKey].GetSubList(c0, c1, newStoreResetsOffsets);
            if (!subRow.IsEmpty())
            {
                store._rows[rowKey - rowOffset] = subRow;
            }
        }

        store.InvalidateRowCache();
        return store;
    }

    public RowDataCollection<T> GetNonEmptyRowData(IRegion region)
    {
        var keys = EnsureSortedRowKeys();
        var startIdx = Array.BinarySearch(keys, region.Top);
        if (startIdx < 0) startIdx = ~startIdx;

        var rowIndicesList = new List<int>();
        var rowDataList = new List<RowData<T>>();

        for (int i = startIdx; i < keys.Length; i++)
        {
            var rowKey = keys[i];
            if (rowKey > region.Bottom)
                break;

            var nonEmptyCols = _rows[rowKey].GetNonEmptyDataBetween(region.Left, region.Right).ToList();
            if (nonEmptyCols.Count == 0)
                continue;

            rowIndicesList.Add(rowKey);
            var colIndices = nonEmptyCols.Select(x => x.itemIndex).ToArray();
            var colData = nonEmptyCols.Select(x => x.data).ToArray();
            rowDataList.Add(new RowData<T>(rowKey, colIndices, colData));
        }

        return new RowDataCollection<T>(rowIndicesList.ToArray(), rowDataList.ToArray());
    }

    public RowDataCollection<T> GetRowData(IRegion region)
    {
        var indices = new int[region.Height];
        var rows = new RowData<T>[region.Height];

        for (int i = 0; i < region.Height; i++)
        {
            var rowIndex = i + region.Top;
            indices[i] = rowIndex;

            if (_rows.TryGetValue(rowIndex, out var rowData))
            {
                var nonEmptyCols = rowData.GetNonEmptyDataBetween(region.Left, region.Right).ToList();
                var colIndices = nonEmptyCols.Select(x => x.itemIndex).ToArray();
                var colData = nonEmptyCols.Select(x => x.data).ToArray();
                rows[i] = new RowData<T>(rowIndex, colIndices, colData);
            }
            else
            {
                rows[i] = new RowData<T>(rowIndex, Array.Empty<int>(), Array.Empty<T>());
            }
        }

        return new RowDataCollection<T>(indices, rows);
    }
}
