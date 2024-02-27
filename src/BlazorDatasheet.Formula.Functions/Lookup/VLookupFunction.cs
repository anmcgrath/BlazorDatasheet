using BlazorDatasheet.DataStructures.Search;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

namespace BlazorDatashet.Formula.Functions.Lookup;

public class VLookupFunction : ISheetFunction
{
    public ParameterDefinition[] GetParameterDefinitions()
    {
        return new[]
        {
            new ParameterDefinition("Lookup", ParameterType.Any, ParameterRequirement.Required),
            new ParameterDefinition("DataSource", ParameterType.Array, ParameterRequirement.Required),
            new ParameterDefinition("Column", ParameterType.Integer, ParameterRequirement.Required),
            new ParameterDefinition("RangeLookup", ParameterType.Logical, ParameterRequirement.Optional)
        };
    }

    public CellValue Call(CellValue[] args, FunctionCallMetaData metaData)
    {
        var isRangeLookup = args.Length > 3 ? args[3].GetValue<bool>() : true;

        for (int i = 1; i < args.Length; i++)
            if (args[i].IsError())
                return args[i];

        var lookupValue = args[0];
        var data = args[1];
        var column = (int)(double)args[2].Data! - 1;
        var nColumns = data.Columns();
        if (column < 0 || column > nColumns - 1)
            return CellValue.Error(ErrorType.Ref, "Column lookup outside of array");

        var nRows = data.Rows();
        var dataArr = (CellValue[][])data.Data!;

        if (isRangeLookup == false)
        {
            for (int row = 0; row < nRows; row++)
            {
                if (dataArr[row][0].IsEqualTo(lookupValue))
                    return dataArr[row][column];
            }

            return CellValue.Error(ErrorType.Na);
        }

        var arrAsList = dataArr.Select(x => x[0]).ToList()!;
        
        var indexSearched = arrAsList.BinarySearchIndexOf(lookupValue);
        if (indexSearched >= 0)
            return dataArr[indexSearched][column];
        else
            indexSearched = ~indexSearched - 1;

        if (indexSearched < 0 || indexSearched > arrAsList.Count - 1)
            return CellValue.Error(ErrorType.Na);

        return dataArr[indexSearched][column];
    }

    public bool AcceptsErrors => false;
}