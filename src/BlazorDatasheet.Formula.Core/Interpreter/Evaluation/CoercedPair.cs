namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

/// <summary>
/// Both operands of a binary operator, coerced to the same type. A struct, since one is made for
/// every arithmetic operation evaluated.
/// </summary>
internal readonly record struct CoercedPair<T>(T Value1, T Value2, bool HasError);

internal static class CoercedPair
{
    public static CoercedPair<double> AsNumbers(CellValue value1, CellValue value2, CellValueCoercer cellValueCoercer)
    {
        double converted1;
        double converted2;
        bool canConvertVal1 = true;
        bool canConvertVal2 = true;

        if (value1.ValueType == CellValueType.Number)
            converted1 = value1.NumberValue;
        else
        {
            canConvertVal1 = cellValueCoercer.TryCoerceNumber(value1, out var val1Num);
            converted1 = val1Num;
        }

        if (value2.ValueType == CellValueType.Number)
            converted2 = value2.NumberValue;
        else
        {
            canConvertVal2 = cellValueCoercer.TryCoerceNumber(value2, out var val2Num);
            converted2 = val2Num;
        }

        var hasError = !canConvertVal1 || !canConvertVal2;
        return new CoercedPair<double>(converted1, converted2, hasError);
    }
}