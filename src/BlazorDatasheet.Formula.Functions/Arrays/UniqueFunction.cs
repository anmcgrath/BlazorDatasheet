using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

public static class UniqueFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("array", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "The range or array to take the unique rows from."),
        new("by_col", ParameterType.Logical, ParameterRequirement.Optional,
            description: "Whether to compare columns rather than rows. False by default."),
        new("exactly_once", ParameterType.Logical, ParameterRequirement.Optional,
            description: "Whether to return only the rows that occur exactly once. False by default.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "UNIQUE",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        returnShape: ReturnShape.Array,
        description: "Returns the distinct rows (or columns) of a range or array, in the order they first appear.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var rows = ArrayFunctionHelpers.Rows(args[0]);
        var byCol = args.Length > 1 && args[1].LogicalValue;
        var exactlyOnce = args.Length > 2 && args[2].LogicalValue;

        if (byCol)
            rows = ArrayFunctionHelpers.Transpose(rows);

        var distinct = new List<CellValue[]>();
        var counts = new List<int>();
        foreach (var row in rows)
        {
            var index = distinct.FindIndex(x => ArrayFunctionHelpers.RowsEqual(x, row));
            if (index < 0)
            {
                distinct.Add(row);
                counts.Add(1);
            }
            else
                counts[index]++;
        }

        var result = new List<CellValue[]>(distinct.Count);
        for (var i = 0; i < distinct.Count; i++)
        {
            if (!exactlyOnce || counts[i] == 1)
                result.Add(distinct[i]);
        }

        var resultRows = result.ToArray();
        if (byCol)
            resultRows = ArrayFunctionHelpers.Transpose(resultRows);
        return CellValue.Array(resultRows);
    }
}
