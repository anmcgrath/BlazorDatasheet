using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatashet.Formula.Functions.Math;

public class SinFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("x",
                ParameterType.Number,
                ParameterRequirement.Required,
                isRepeating: false)
        };
    }

    public object? Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var val = args[0];
        if (val.ValueType == CellValueType.Error)
            return val.Data;

        if (val.IsEmpty)
            return 0; // Math.Sin(0);

        if (val.ValueType != CellValueType.Number)
            return new FormulaError(ErrorType.Value);

        return System.Math.Sin(val.GetValue<double>());
    }

    public bool AcceptsErrors => false;
}