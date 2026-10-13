using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

internal static class ArrayFunctionHelpers
{
    internal static CellValue[][] Rows(CellValue array) => (CellValue[][])array.Data!;

    internal static int ColumnCount(CellValue[][] rows) => rows.Length == 0 ? 0 : rows[0].Length;

    internal static CellValue[][] Transpose(CellValue[][] rows)
    {
        var cols = ColumnCount(rows);
        var result = new CellValue[cols][];
        for (var c = 0; c < cols; c++)
        {
            result[c] = new CellValue[rows.Length];
            for (var r = 0; r < rows.Length; r++)
                result[c][r] = rows[r][c];
        }

        return result;
    }

    internal static bool RowsEqual(CellValue[] a, CellValue[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (var i = 0; i < a.Length; i++)
        {
            if (!a[i].IsEqualTo(b[i]))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Reads an element of an include/by array as a logical, or returns the error to report.
    /// </summary>
    internal static bool TryAsLogical(CellValue value, out bool result, out CellValue error)
    {
        error = CellValue.Empty;
        result = false;
        switch (value.ValueType)
        {
            case CellValueType.Logical:
                result = value.LogicalValue;
                return true;
            case CellValueType.Number:
                result = value.NumberValue != 0;
                return true;
            case CellValueType.Empty:
                return true;
            case CellValueType.Error:
                error = value;
                return false;
            default:
                error = CellValue.Error(ErrorType.Value);
                return false;
        }
    }

    /// <summary>
    /// Whether the value is a single number, as given for a sort order.
    /// </summary>
    internal static bool TryAsSingleNumber(CellValue array, out double number)
    {
        number = 0;
        var rows = Rows(array);
        if (rows.Length != 1 || rows[0].Length != 1 || rows[0][0].ValueType != CellValueType.Number)
            return false;
        number = rows[0][0].NumberValue;
        return true;
    }

    /// <summary>
    /// Sorts the given indices by the keys, in order of the keys, keeping the original order of ties.
    /// Each key is a column of values, one per index, with its own direction (1 ascending, -1 descending).
    /// </summary>
    internal static int[] SortIndices(int count, IReadOnlyList<(CellValue[] keys, int order)> sortKeys)
    {
        var indices = Enumerable.Range(0, count).ToArray();
        Array.Sort(indices, (a, b) =>
        {
            foreach (var (keys, order) in sortKeys)
            {
                var cmp = keys[a].CompareTo(keys[b]) * order;
                if (cmp != 0)
                    return cmp;
            }

            return a.CompareTo(b);
        });
        return indices;
    }
}
