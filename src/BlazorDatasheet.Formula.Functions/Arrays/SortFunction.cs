using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

public static class SortFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("array", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "The range or array to sort."),
        new("sort_index", ParameterType.Integer, ParameterRequirement.Optional,
            description: "The number of the row or column to sort by, starting at 1. 1 by default."),
        new("sort_order", ParameterType.Integer, ParameterRequirement.Optional,
            description: "1 for ascending (the default), -1 for descending."),
        new("by_col", ParameterType.Logical, ParameterRequirement.Optional,
            description: "Whether to sort the columns rather than the rows. False by default.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SORT",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        returnShape: ReturnShape.Array,
        description: "Returns the rows (or columns) of a range or array sorted by one of its columns (or rows).");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var rows = ArrayFunctionHelpers.Rows(args[0]);
        var sortIndex = args.Length > 1 ? (int)args[1].NumberValue : 1;
        var sortOrder = args.Length > 2 ? (int)args[2].NumberValue : 1;
        var byCol = args.Length > 3 && args[3].LogicalValue;

        if (sortOrder != 1 && sortOrder != -1)
            return CellValue.Error(ErrorType.Value, "Sort order must be 1 or -1");

        if (byCol)
            rows = ArrayFunctionHelpers.Transpose(rows);

        if (rows.Length == 0)
            return CellValue.Array(rows);

        if (sortIndex < 1 || sortIndex > ArrayFunctionHelpers.ColumnCount(rows))
            return CellValue.Error(ErrorType.Value, "Sort index is outside the array");

        var keys = rows.Select(r => r[sortIndex - 1]).ToArray();
        var order = ArrayFunctionHelpers.SortIndices(rows.Length, [(keys, sortOrder)]);
        var sorted = order.Select(i => rows[i]).ToArray();

        if (byCol)
            sorted = ArrayFunctionHelpers.Transpose(sorted);
        return CellValue.Array(sorted);
    }
}
