using BlazorDatasheet.Data;
using BlazorDatasheet.FormulaEngine.Interpreter.Syntax;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.FormulaEngine.Interpreter.Functions;

public class ParameterTypeConversion
{
    private Environment _environment;

    public ParameterTypeConversion(Environment environment)
    {
        _environment = environment;
    }

    public object ConvertTo(ParameterType type, object val)
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

        if (val is IReadOnlyCell cell)
            return ToText(cell.GetValue());

        if (val is BRange range)
            return new FormulaError(ErrorType.Value);

        return val.ToString();
    }

    private object ToNumber(object val)
    {
        if (ConvertsToNumber(val))
            return Convert.ToDouble(val);
        return new FormulaError(ErrorType.Value);
    }

    /// <summary>
    /// Converts an object to an IEnumerable<double> or a formula error
    /// </summary>
    /// <param name="arg"></param>
    /// <returns></returns>
    public object? ToNumberSequence(object? arg)
    {
        if (arg == null)
            return Enumerable.Empty<double>();

        if (arg is double d)
            return new[] { d };

        if (ConvertsToNumber(arg))
            return new[] { Convert.ToDouble(arg) };

        if (arg is IReadOnlyCell cell)
        {
            if (cell.GetValue() is double value)
                return new[] { value };
        }

        if (arg is BRange range)
        {
            var nonEmptyCells = range.GetNonEmptyCells();
            var numbers = new List<double>();
            foreach (var numberCell in nonEmptyCells)
            {
                var num = numberCell.GetValue<double?>();
                if (num != null)
                    numbers.Add(num.Value);
            }

            return numbers;
        }

        return new FormulaError(ErrorType.Num);
    }

    public object? ToLogical(object? arg)
    {
        if (arg is bool b)
            return b;
        if (arg is double d)
        {
            return d != 0;
        }

        if (arg is IReadOnlyCell cell)
        {
            return ToLogical(cell.GetValue());
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