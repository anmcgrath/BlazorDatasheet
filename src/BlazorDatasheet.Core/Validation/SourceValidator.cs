using BlazorDatasheet.Core.Interfaces;

namespace BlazorDatasheet.Core.Validation;

public class SourceValidator : IDataValidator
{
    public List<string> ValidationSource { get; }
    public bool IsStrict { get; }
    public string Message => "The value must be one of the source values.";

    /// <summary>
    /// Ensures that the value is equal to one of the validation sources
    /// </summary>
    /// <param name="validationSource"></param>
    /// <param name="isStrict"></param>
    public SourceValidator(List<string> validationSource, bool isStrict)
    {
        ValidationSource = validationSource;
        IsStrict = isStrict;
    }

    public bool IsValid(object? val)
    {
        try
        {
            if (val == null)
                return false;
            var valStr = val.ToString();
            var isValid = ValidationSource.Any(x => string.Compare(x, valStr, StringComparison.Ordinal) == 0);
            return isValid;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}