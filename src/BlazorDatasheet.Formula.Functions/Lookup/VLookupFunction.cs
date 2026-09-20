using BlazorDatasheet.DataStructures.Search;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

namespace BlazorDatashet.Formula.Functions.Lookup;

public static class VLookupFunction
{
    private static readonly ParameterDefinition[] Parameters =
    [
        new("Lookup", ParameterType.Any, ParameterRequirement.Required, shape: ParameterShape.ScalarOrArray,
            description: "The value to search for."),
        new("DataSource", ParameterType.Array, ParameterRequirement.Required, shape: ParameterShape.Array,
            description: "The range to search. The first column is searched for the lookup value."),
        new("Column", ParameterType.Integer, ParameterRequirement.Required,
            description: "The number of the column in the range to return the value from, starting at 1."),
        new("RangeLookup", ParameterType.Logical, ParameterRequirement.Optional,
            description: "Whether to find an approximate match, which needs the first column to be sorted, rather " +
                         "than an exact match. Approximate by default.")
    ];

    public static FunctionDescriptor Descriptor { get; } = new(
        name: "VLOOKUP",
        parameterDefinitions: Parameters,
        invoker: Evaluate,
        acceptsErrors: false,
        isVolatile: false,
        description: "Searches the first column of a range for a value and returns the value in the same row of " +
                     "another column.");

    private static CellValue Evaluate(ReadOnlySpan<CellValue> args, FunctionCallMetaData metaData)
    {
        var isRangeLookup = args.Length > 3 ? args[3].GetValue<bool>() : true;

        for (int i = 1; i < args.Length; i++)
        {
            if (args[i].IsError())
                return args[i];
        }

        var lookupValue = args[0];
        var data = args[1];
        var column = (int)(double)args[2].Data! - 1;
        var nColumns = data.Columns();
        if (column < 0 || column > nColumns - 1)
            return CellValue.Error(ErrorType.Ref, "Column lookup outside of array");

        var nRows = data.Rows();
        var dataArr = (CellValue[][])data.Data!;

        if (!isRangeLookup)
        {
            for (int row = 0; row < nRows; row++)
            {
                if (dataArr[row][0].IsEqualTo(lookupValue))
                    return dataArr[row][column];
            }

            return CellValue.Error(ErrorType.Na);
        }

        var arrAsList = dataArr.Select(x => x[0]).ToList();
        var indexSearched = arrAsList.BinarySearchIndexOf(lookupValue);
        if (indexSearched >= 0)
            return dataArr[indexSearched][column];

        indexSearched = ~indexSearched - 1;

        if (indexSearched < 0 || indexSearched > arrAsList.Count - 1)
            return CellValue.Error(ErrorType.Na);

        return dataArr[indexSearched][column];
    }
}
