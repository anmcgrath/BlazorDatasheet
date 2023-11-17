using BlazorDatasheet.DataStructures.Util;
using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class FuncArg
{
    public object Value { get; init; }
    public ParameterDefinition ParameterDefinition { get; init; }

    public FuncArg(object value, ParameterDefinition parameterDefinition)
    {
        Value = value;
        ParameterDefinition = parameterDefinition;
    }

    public IEnumerable<CellValue> Flatten()
    {
        switch (ParameterDefinition.Dimensionality)
        {
            case ParameterDimensionality.Scalar:
                if (ParameterDefinition.IsRepeating)
                    return (CellValue[])(Value);
                else
                    return new[] { (CellValue)Value };
            case ParameterDimensionality.Range:
                if (ParameterDefinition.IsRepeating)
                {
                    var arr = (CellValue[][][])Value;
                    return arr.SelectMany(x => x.SelectMany(y => y));
                }
                else
                {
                    var arr = (CellValue[][])Value;
                    return arr.SelectMany(x => x);
                }
        }

        return Array.Empty<CellValue>();
    }

    /// <summary>
    /// Returns a single CellValue. If the parameter dimensionality is a range, or the parameter is repeating,
    /// returns a cell error.
    /// </summary>
    /// <returns></returns>
    public CellValue AsScalar()
    {
        if (ParameterDefinition.Dimensionality != ParameterDimensionality.Scalar ||
            ParameterDefinition.IsRepeating)
            return CellValue.Error(new FormulaError(ErrorType.Ref));

        return (CellValue)Value;
    }

    public CellValue[][] AsRange()
    {
        return ToRange(Value, ParameterDefinition);
    }

    private CellValue[][] ToRange(object obj, ParameterDefinition parameterDefinition)
    {
        if (parameterDefinition.Dimensionality == ParameterDimensionality.Scalar)
        {
            if (parameterDefinition.IsRepeating)
                return new[] { new[] { CellValue.Error(new FormulaError(ErrorType.Ref)) } };

            return new[] { new[] { (CellValue)obj } };
        }
        else if (parameterDefinition.Dimensionality == ParameterDimensionality.Range)
        {
            if (parameterDefinition.IsRepeating)
                return new[] { new[] { CellValue.Error(new FormulaError(ErrorType.Ref)) } };

            return (CellValue[][])obj;
        }

        // Error - shouldn't encounter this
        return new[] { new[] { CellValue.Error(new FormulaError(ErrorType.Ref)) } };
    }

    public CellValue[][][] AsRepeatingRange()
    {
        if (ParameterDefinition.Dimensionality == ParameterDimensionality.Scalar)
        {
            if (ParameterDefinition.IsRepeating)
            {
                var arr = (CellValue[])Value;
                return arr.Select(x => ToRange(x, ParameterDefinition)).ToArray();
            }
            else
            {
                return new[] { ToRange((CellValue)Value, ParameterDefinition) };
            }
        }
        else
        {
            if (ParameterDefinition.IsRepeating)
                return (CellValue[][][])Value;
            else
            {
                return new[] { (CellValue[][])Value };
            }
        }
    }
}