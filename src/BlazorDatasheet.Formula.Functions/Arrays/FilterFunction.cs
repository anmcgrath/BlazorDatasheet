using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

public static class FilterFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("array", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "The range or array to filter."),
        new("include", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "A column of logicals with one entry per row to keep, or a row with one per column."),
        new("if_empty", ParameterType.Any, ParameterRequirement.Optional, shape: ParameterShape.ScalarOrArray,
            description: "The value to return when nothing is kept. #CALC! by default.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "FILTER",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        returnShape: ReturnShape.Array,
        description: "Returns the rows (or columns) of a range or array for which the matching include value is true.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var rows = ArrayFunctionHelpers.Rows(args[0]);
        var include = ArrayFunctionHelpers.Rows(args[1]);
        var rowCount = rows.Length;
        var colCount = ArrayFunctionHelpers.ColumnCount(rows);
        var includeRows = include.Length;
        var includeCols = ArrayFunctionHelpers.ColumnCount(include);

        CellValue[][] result;
        if (includeCols == 1 && includeRows == rowCount)
        {
            var kept = new List<CellValue[]>();
            for (var r = 0; r < rowCount; r++)
            {
                if (!ArrayFunctionHelpers.TryAsLogical(include[r][0], out var keep, out var error))
                    return error;
                if (keep)
                    kept.Add(rows[r]);
            }

            result = kept.ToArray();
        }
        else if (includeRows == 1 && includeCols == colCount)
        {
            var keptCols = new List<int>();
            for (var c = 0; c < colCount; c++)
            {
                if (!ArrayFunctionHelpers.TryAsLogical(include[0][c], out var keep, out var error))
                    return error;
                if (keep)
                    keptCols.Add(c);
            }

            result = keptCols.Count == 0
                ? []
                : rows.Select(row => keptCols.Select(c => row[c]).ToArray()).ToArray();
        }
        else
            return CellValue.Error(ErrorType.Value, "The include array does not match the array's size");

        if (result.Length == 0 && args.Length > 2)
            return args[2];

        return CellValue.Array(result);
    }
}
