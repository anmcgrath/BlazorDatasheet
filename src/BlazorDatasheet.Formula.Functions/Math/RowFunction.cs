using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatashet.Formula.Functions.Math;

public static class RowFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("Reference", ParameterType.Any, ParameterRequirement.Optional)
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "ROW",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false);

    private static CellValue Evaluate(CellValue[] args, FunctionCallMetaData metaData)
    {
        if (args.Length == 0)
            return GetCallerRow(metaData);

        return GetReferenceRow(args[0]);
    }

    private static CellValue GetCallerRow(FunctionCallMetaData metaData)
    {
        if (metaData.CallingRowIndex == null)
            return CellValue.Error(ErrorType.Value);

        return CellValue.Number(metaData.CallingRowIndex.Value + 1);
    }

    private static CellValue GetReferenceRow(CellValue arg)
    {
        if (arg.ValueType != CellValueType.Reference)
            return CellValue.Error(ErrorType.Value);

        return arg.Data switch
        {
            CellReference cellReference => CellValue.Number(cellReference.RowIndex + 1),
            Reference reference => CellValue.Number(reference.Region.Top + 1),
            _ => CellValue.Error(ErrorType.Value)
        };
    }
}
