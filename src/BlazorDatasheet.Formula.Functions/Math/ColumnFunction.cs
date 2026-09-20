using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatashet.Formula.Functions.Math;

public static class ColumnFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("Reference", ParameterType.Any, ParameterRequirement.Optional,
            description: "The cell or range to return the column number of. The cell containing the formula is used " +
                         "when it is left out.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "COLUMN",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Returns the column number of a reference.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        if (args.Length == 0)
            return GetCallerColumn(metaData);

        return GetReferenceColumn(args[0]);
    }

    private static CellValue GetCallerColumn(FunctionCallMetaData metaData)
    {
        if (metaData.CallingColumnIndex == null)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Number(metaData.CallingColumnIndex.Value + 1);
    }

    private static CellValue GetReferenceColumn(CellValue arg)
    {
        if (arg.ValueType != CellValueType.Reference)
            return CellValue.Error(ErrorType.Value);

        return arg.Data switch
        {
            CellReference cellReference => CellValue.Number(cellReference.ColIndex + 1),
            Reference reference => CellValue.Number(reference.Region.Left + 1),
            _ => CellValue.Error(ErrorType.Value)
        };
    }
}
