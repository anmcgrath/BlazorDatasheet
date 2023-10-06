using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Validation;

public class NumberValidator : IDataValidator
{
    public NumberValidator(bool isStrict)
    {
        IsStrict = isStrict;
    }

    public bool IsValid(object? value)
    {
        if (value == null)
            return false;
        var val = value.ToString();
        return double.TryParse(val, out double res);
    }

    public bool IsStrict { get; }
    public string Message => "The value must be a valid number.";
}