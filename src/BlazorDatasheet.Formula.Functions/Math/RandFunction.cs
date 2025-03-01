using BlazorDatasheet.Formula.Core;

namespace BlazorDatashet.Formula.Functions.Math;

public class RandFunction : ISheetFunction
{
    private Random _random;

    public ParameterDefinition[] GetParameterDefinitions()
    {
        return [];
    }

    public RandFunction()
    {
        _random = new Random();
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        return CellValue.Number(_random.NextDouble());
    }

    public bool AcceptsErrors => false;
    public bool IsVolatile => true;
}