using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

public static class SortByFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("array", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "The range or array to sort."),
        new("by_array", ParameterType.Array, ParameterRequirement.Required, isRepeating: true,
            shape: ParameterShape.ScalarOrArray,
            description: "A row or column of values to sort by, optionally followed by 1 (ascending) or " +
                         "-1 (descending). Further pairs break ties.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SORTBY",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        returnShape: ReturnShape.Array,
        description: "Returns a range or array sorted by the values of one or more other ranges or arrays.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var rows = ArrayFunctionHelpers.Rows(args[0]);
        var rowCount = rows.Length;
        var colCount = ArrayFunctionHelpers.ColumnCount(rows);

        // each by-array is a column (sorting rows) or a row (sorting columns); all must agree
        bool? byRows = null;
        var sortKeys = new List<(CellValue[] keys, int order)>();
        var i = 1;
        while (i < args.Length)
        {
            var by = ArrayFunctionHelpers.Rows(args[i]);
            var byRowCount = by.Length;
            var byColCount = ArrayFunctionHelpers.ColumnCount(by);

            CellValue[] keys;
            if (byColCount == 1 && byRowCount == rowCount && byRows != false)
            {
                byRows = true;
                keys = by.Select(r => r[0]).ToArray();
            }
            else if (byRowCount == 1 && byColCount == colCount && byRows != true)
            {
                byRows = false;
                keys = by[0];
            }
            else
                return CellValue.Error(ErrorType.Value, "The by_array does not match the array's size");

            var order = 1;
            i++;
            if (i < args.Length && ArrayFunctionHelpers.TryAsSingleNumber(args[i], out var orderNumber))
            {
                if (orderNumber != 1 && orderNumber != -1)
                    return CellValue.Error(ErrorType.Value, "Sort order must be 1 or -1");
                order = (int)orderNumber;
                i++;
            }

            sortKeys.Add((keys, order));
        }

        if (rowCount == 0 || sortKeys.Count == 0)
            return CellValue.Array(rows);

        if (byRows == true)
        {
            var order = ArrayFunctionHelpers.SortIndices(rowCount, sortKeys);
            return CellValue.Array(order.Select(r => rows[r]).ToArray());
        }

        var colOrder = ArrayFunctionHelpers.SortIndices(colCount, sortKeys);
        var result = new CellValue[rowCount][];
        for (var r = 0; r < rowCount; r++)
            result[r] = colOrder.Select(c => rows[r][c]).ToArray();
        return CellValue.Array(result);
    }
}
