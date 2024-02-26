using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Validation;

public class NumberValidator : IDataValidator
{
    public NumberValidator(bool isStrict)
    {
        IsStrict = isStrict;
    }

    public bool IsValid(CellValue value)
    {
        return value.ValueType == CellValueType.Number;
    }

    public bool IsStrict { get; }
    public string Message => "The value must be a valid number.";
}