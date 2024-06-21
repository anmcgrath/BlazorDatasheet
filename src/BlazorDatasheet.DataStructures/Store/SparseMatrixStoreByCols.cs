using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

public class SparseMatrixStoreByCols<T> : IMatrixDataStore<T>
{
    private readonly T _defaultValueIfEmpty;

    /// <summary>
    /// Matrix columns, with the key being the column index
    /// </summary>
    private readonly Dictionary<int, SColumn<T>> _columns = new();

    public SparseMatrixStoreByCols(T defaultValueIfEmpty = default(T))
    {
        _defaultValueIfEmpty = defaultValueIfEmpty;
    }

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
            return _defaultValueIfEmpty;

        return column!.Get(row);
    }

    public MatrixRestoreData<T> Set(int row, int col, T value)
    {
        var colExists = _columns.TryGetValue(col, out var column);
        var restoreData = new MatrixRestoreData<T>();

        if (!colExists)
        {
            restoreData.DataRemoved = new()
            {
                (row, col, _defaultValueIfEmpty)
            };

            column = new SColumn<T>(col, _defaultValueIfEmpty);
            _columns.Add(col, column);
        }
        else
        {
            restoreData.DataRemoved = new List<(int row, int col, T? data)>()
            {
                (row, col, column!.Get(row))
            };
        }

        column.Set(row, value);
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

    public MatrixRestoreData<T> InsertRowAt(int row, int count = 1)
    {
        foreach (var column in _columns.Values)
        {
            column.InsertRowAt(row, count);
        }

        return new MatrixRestoreData<T>()
        {
            Shifts = [new AppliedShift(Axis.Row, row, count)]
        };
    }

    public MatrixRestoreData<T> InsertColAt(int col, int count)
    {
        var currentColumns = _columns.ToList();
        List<(int colIndex, SColumn<T> column)> columnsToReAdd = new List<(int colIndex, SColumn<T> column)>();
        foreach (var kp in currentColumns)
        {
            if (kp.Key >= col)
            {
                _columns.Remove(kp.Key);
                columnsToReAdd.Add((kp.Key + count, kp.Value));
            }
        }

        foreach (var c in columnsToReAdd)
        {
            c.column.ColumnIndex = c.colIndex;
            _columns.Add(c.colIndex, c.column);
        }

        for (int i = 0; i < count; i++)
        {
            _columns.Add(col + i, new SColumn<T>(col + i, _defaultValueIfEmpty));
        }
        
        return new MatrixRestoreData<T>()
        {
            Shifts = [new AppliedShift(Axis.Col, col, count)]
        };
    }

    public MatrixRestoreData<T> RemoveColAt(int col, int count)
    {
        var deleted = new List<(int row, int col, T? data)>();

        for (int i = 0; i < count; i++)
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
                columnsToReAdd.Add((kp.Key - count, kp.Value));
            }
        }

        foreach (var c in columnsToReAdd)
        {
            c.column.ColumnIndex = c.colIndex;
            _columns.Add(c.colIndex, c.column);
        }

        return new MatrixRestoreData<T>()
        {
            DataRemoved = deleted,
            Shifts = [new AppliedShift(Axis.Col, col, -count)]
        };
    }

    public int GetNextNonBlankRow(int row, int col)
    {
        if (!_columns.ContainsKey(col))
            return -1;
        return _columns[col].GetNextNonEmptyRow(row);
    }

    public int GetNextNonBlankColumn(int row, int col)
    {
        var cols = _columns.Where(x => x.Key > col)
            .OrderBy(x => x.Key);
        foreach (var kp in cols)
            if (kp.Value.ContainsRow(row))
                return kp.Key;

        return -1;
    }

    public MatrixRestoreData<T> RemoveRowAt(int row, int count)
    {
        var deleted = new List<(int row, int col, T?)>();
        foreach (var column in _columns.Values)
            deleted.AddRange(column.DeleteRowAt(row, count)!);
        return new MatrixRestoreData<T>()
        {
            DataRemoved = deleted,
            Shifts = [new AppliedShift(Axis.Row, row, -count)]
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
            var nonEmptyInRow = kp.Value.GetNonEmptyRowsBetween(r0, r1);
            nonEmptyPositions.AddRange(nonEmptyInRow.Select(x => new CellPosition(x, kp.Key)));
        }

        return nonEmptyPositions;
    }

    public IEnumerable<(int row, int col, T data)> GetNonEmptyData(IRegion region) =>
        GetNonEmptyData(region.Top, region.Bottom, region.Left, region.Right);

    public T[][] GetData(IRegion region)
    {
        var result = new T[region.Height][];
        for (int i = 0; i < region.Height; i++)
        {
            result[i] = new T[region.Width];
            for (int j = 0; j < region.Width; j++)
            {
                result[i][j] = Get(i, j);
            }
        }

        return result;
    }

    public IMatrixDataStore<T> GetSubStore(IRegion region, bool newStoreResetsOffsets = true)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get non empty data that exist in the bounds given
    /// </summary>
    /// <param name="r0">The lower row bound</param>
    /// <param name="r1">The upper row bound</param>
    /// <param name="c0">The lower col bound</param>
    /// <param name="c1">The upper col bound</param>
    /// <returns></returns>
    public IEnumerable<(int row, int col, T data)> GetNonEmptyData(int r0, int r1, int c0, int c1)
    {
        List<(int row, int col, T data)> nonEmptyData = new();
        foreach (var kp in _columns)
        {
            if (kp.Key < c0 || kp.Key > c1)
                continue;
            var nonEmptyInRow = kp.Value.GetNonEmptyDataBetween(r0, r1);
            nonEmptyData.AddRange(nonEmptyInRow.Select(x => (x.row, kp.Key, x.data)));
        }

        return nonEmptyData;
    }

    public IEnumerable<CellPosition> GetNonEmptyPositions(IRegion region)
    {
        return GetNonEmptyPositions(region.Top, region.Bottom, region.Left, region.Right);
    }

    public RowDataCollection<T> GetNonEmptyRowData(IRegion region)
    {
        throw new NotImplementedException();
    }

    public RowDataCollection<T> GetRowData(IRegion region)
    {
        throw new NotImplementedException();
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
        foreach (var shift in restoreData.Shifts)
        {
            if (shift.Amount > 0)
            {
                if(shift.Axis == Axis.Col)
                    RemoveColAt(shift.Index, shift.Amount);
                else
                    RemoveRowAt(shift.Index, shift.Amount);
            }
            else
            {
                if(shift.Axis == Axis.Col)
                    InsertColAt(shift.Index, -shift.Amount);
                else
                    InsertRowAt(shift.Index, -shift.Amount);
            }
        }
        
        foreach (var pt in restoreData.DataRemoved)
            Set(pt.row, pt.col, pt.data);
    }
}