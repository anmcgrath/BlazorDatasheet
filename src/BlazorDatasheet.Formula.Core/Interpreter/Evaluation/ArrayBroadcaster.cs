using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

/// <summary>
/// Lifts scalar operations over arrays, following Excel's dynamic array rules: a 1x1 value broadcasts
/// to any shape, a single row or column is repeated along its missing dimension, and where two
/// arrays disagree in a dimension the result takes the larger size with #N/A outside the smaller.
/// </summary>
internal static class ArrayBroadcaster
{
    /// <summary>
    /// Whether the value is an array or a range reference, i.e. something with a shape to lift over.
    /// </summary>
    internal static bool IsArrayLike(CellValue value)
    {
        if (value.ValueType == CellValueType.Array)
            return true;
        return value.ValueType == CellValueType.Reference &&
               ((Reference)value.Data!).Kind == ReferenceKind.Range;
    }

    /// <summary>
    /// Turns a range reference into the array of its values. Anything else is returned unchanged.
    /// </summary>
    internal static CellValue Materialize(CellValue value, IEnvironment environment)
    {
        if (value.ValueType == CellValueType.Reference &&
            ((Reference)value.Data!).Kind == ReferenceKind.Range)
            return CellValue.Array(environment.GetRangeValues((Reference)value.Data!));
        return value;
    }

    internal static (int rows, int cols) Shape(CellValue value)
    {
        if (value.ValueType != CellValueType.Array)
            return (1, 1);
        return (value.Rows(), value.Columns());
    }

    /// <summary>
    /// The shape of the result of combining values of the given shapes.
    /// </summary>
    internal static (int rows, int cols) BroadcastShape(ReadOnlySpan<(int rows, int cols)> shapes)
    {
        var rows = 0;
        var cols = 0;
        foreach (var shape in shapes)
        {
            rows = Math.Max(rows, shape.rows);
            cols = Math.Max(cols, shape.cols);
        }

        return (rows, cols);
    }

    /// <summary>
    /// The element of <paramref name="value"/> at the result position. A scalar is its own element, a
    /// dimension of size one is repeated, and a position outside the value is #N/A.
    /// </summary>
    internal static CellValue ElementAt(CellValue value, int row, int col)
    {
        if (value.ValueType != CellValueType.Array)
            return value;

        var data = (CellValue[][])value.Data!;
        if (data.Length == 0)
            return CellValue.Error(ErrorType.Na);

        var r = data.Length == 1 ? 0 : row;
        if (r >= data.Length)
            return CellValue.Error(ErrorType.Na);

        var rowData = data[r];
        if (rowData.Length == 0)
            return CellValue.Error(ErrorType.Na);

        var c = rowData.Length == 1 ? 0 : col;
        if (c >= rowData.Length)
            return CellValue.Error(ErrorType.Na);

        return rowData[c];
    }

    internal static CellValue Map(CellValue left, CellValue right, Func<CellValue, CellValue, CellValue> op)
    {
        var (rows, cols) = BroadcastShape([Shape(left), Shape(right)]);
        var result = new CellValue[rows][];
        for (var r = 0; r < rows; r++)
        {
            var row = new CellValue[cols];
            for (var c = 0; c < cols; c++)
                row[c] = op(ElementAt(left, r, c), ElementAt(right, r, c));
            result[r] = row;
        }

        return CellValue.Array(result);
    }

    internal static CellValue Map(CellValue value, Func<CellValue, CellValue> op)
    {
        var (rows, cols) = Shape(value);
        var result = new CellValue[rows][];
        for (var r = 0; r < rows; r++)
        {
            var row = new CellValue[cols];
            for (var c = 0; c < cols; c++)
                row[c] = op(ElementAt(value, r, c));
            result[r] = row;
        }

        return CellValue.Array(result);
    }
}
