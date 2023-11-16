using BlazorDatasheet.DataStructures.Cells;
using BlazorDatasheet.Formula.Core.Interpreter.Functions;

namespace BlazorDatashet.Formula.Functions.Math;

public class SinFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("x",
                ParameterType.Number,
                ParameterDimensionality.Scalar,
                ParameterRequirement.Required,
                false)
        };
    }

    public object Call(FuncArg[] args)
    {
        var val = (args.First().AsScalar());
        if (val.ValueType == CellValueType.Error)
            return val.Data;

        if (val.IsEmpty)
            return 0; // Math.Sin(0);
        else
        {
            
        }
            return System.Math.Sin(val.GetValue<double>());
    }

    public bool AcceptsErrors => false;
}