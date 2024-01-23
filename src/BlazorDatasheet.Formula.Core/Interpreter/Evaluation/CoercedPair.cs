namespace BlazorDatasheet.Formula.Core.Interpreter.Evaluation;

internal class CoercedPair<T>
{
    public T Value1 { get; }
    public T Value2 { get; }
    public bool HasError { get; }

    public CoercedPair(T value1, T value2, bool hasError)
    {
        Value1 = value1;
        Value2 = value2;
        HasError = hasError;
    }
}

internal class CoercedPair
{
    public static CoercedPair<double> AsNumbers(CellValue value1, CellValue value2, CellValueCoercer cellValueCoercer)
    {
        double converted1;
        double converted2;
        bool canConvertVal1 = true;
        bool canConvertVal2 = true;

        if (value1.ValueType == CellValueType.Number)
            converted1 = value1.GetValue<double>();
        else
        {
            canConvertVal1 = cellValueCoercer.TryCoerceNumber(value1, out var val1Num);
            converted1 = val1Num;
        }

        if (value2.ValueType == CellValueType.Number)
            converted2 = value2.GetValue<double>();
        else
        {
            canConvertVal2 = cellValueCoercer.TryCoerceNumber(value2, out var val2Num);
            converted2 = val2Num;
        }

        var hasError = !canConvertVal1 || !canConvertVal2;
        return new CoercedPair<double>(converted1, converted2, hasError);
    }
}