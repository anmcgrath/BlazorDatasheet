using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Formula.Functions.Arrays;

public static class SequenceFunction
{
    private const int MaxCells = 1_000_000;

    private static readonly ParameterDefinition[] Parameters =
    [
        new("rows", ParameterType.Integer, ParameterRequirement.Required,
            description: "The number of rows to return."),
        new("columns", ParameterType.Integer, ParameterRequirement.Optional,
            description: "The number of columns to return. 1 by default."),
        new("start", ParameterType.Number, ParameterRequirement.Optional,
            description: "The first number in the sequence. 1 by default."),
        new("step", ParameterType.Number, ParameterRequirement.Optional,
            description: "The amount each number increases by. 1 by default.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "SEQUENCE",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        returnShape: ReturnShape.Array,
        description: "Returns an array of sequential numbers, filled row by row.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var rows = (int)args[0].NumberValue;
        var cols = args.Length > 1 ? (int)args[1].NumberValue : 1;
        var start = args.Length > 2 ? args[2].NumberValue : 1;
        var step = args.Length > 3 ? args[3].NumberValue : 1;

        if (rows < 1 || cols < 1)
            return CellValue.Error(ErrorType.Value, "Rows and columns must be at least 1");
        if ((long)rows * cols > MaxCells)
            return CellValue.Error(ErrorType.Num, "The sequence is too large");

        var result = new CellValue[rows][];
        var n = 0;
        for (var r = 0; r < rows; r++)
        {
            result[r] = new CellValue[cols];
            for (var c = 0; c < cols; c++)
                result[r][c] = CellValue.Number(start + step * n++);
        }

        return CellValue.Array(result);
    }
}
