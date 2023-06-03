using BlazorDatasheet.Formula.Core.Interpreter.Syntax;

namespace BlazorDatasheet.Formula.Core.Interpreter.Functions;

public class ParameterTypeConversion
{
    private IEnvironment _environment;

    public ParameterTypeConversion(IEnvironment environment)
    {
        _environment = environment;
    }

    internal object? ConvertTo(ParameterType type, object? val)
    {
        switch (type)
        {
            case ParameterType.Number:
                return ToNumber(val);
            case ParameterType.NumberSequence:
                return ToNumberSequence(val);
            case ParameterType.Logical:
                return ToLogical(val);
            case ParameterType.Any:
                return val;
            case ParameterType.Text:
                return ToText(val);
        }

        return new FormulaError(ErrorType.Value);
    }

    private object? ToText(object? val)
    {
        if (val == null)
            return string.Empty;

        if (val is FormulaError e)
            return e;

        if (val is string t)
            return t;

        if (val is CellAddress addr)
            return ToText(_environment.GetCellValue(addr.Row, addr.Col));

        if (val is RangeAddress or ColumnAddress or RowAddress)
            return new FormulaError(ErrorType.Value);

        return val.ToString();
    }

    private object ToNumber(object? val)
    {
        if (val is CellAddress addr)
            val = _environment.GetCellValue(addr.Row, addr.Col);

        if (ConvertsToNumber(val))
            return Convert.ToDouble(val);
        return new FormulaError(ErrorType.Value);
    }

    /// <summary>
    /// Converts an object to an IEnumerable<double> or a formula error
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    private object? ToNumberSequence(object? arg)
    {
        if (arg == null)
            return Enumerable.Empty<double>();

        if (arg is double d)
            return new[] { d };

        if (ConvertsToNumber(arg))
            return new[] { Convert.ToDouble(arg) };

        if (arg is CellAddress cellAddr)
        {
            var cellVal = _environment.GetCellValue(cellAddr.Row, cellAddr.Col);

            if (cellVal is double value)
                return new[] { value };
        }

        if (arg is RangeAddress rangeAddress)
        {
            return _environment.GetNumbersInRange(rangeAddress);
        }

        if (arg is ColumnAddress columnAddress)
        {
            return _environment.GetNumbersInRange(columnAddress);
        }

        if (arg is RowAddress rowAddress)
        {
            return _environment.GetNumbersInRange(rowAddress);
        }

        return new FormulaError(ErrorType.Num);
    }

    private object? ToLogical(object? arg)
    {
        if (arg is bool b)
            return b;
        if (arg is double d)
        {
            return d != 0;
        }

        if (arg is CellAddress addr)
        {
            return ToLogical(_environment.GetCellValue(addr.Row, addr.Col));
        }

        return new FormulaError(ErrorType.Value);
    }

    private bool ConvertsToNumber(object? o)
    {
        if (o == null)
            return false;

        var type = o.GetType();
        return type == typeof(double) ||
               type == typeof(bool) ||
               type == typeof(int) ||
               type == typeof(decimal);
    }

    private double BoolToNumber(bool value)
    {
        return value ? 1 : 0;
    }
}