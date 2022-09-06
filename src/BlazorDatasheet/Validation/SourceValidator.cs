using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Validation;

public class SourceValidator : IDataValidator
{
    public List<string> ValidationSource { get; }
    public bool IsStrict { get; private set; }

    public SourceValidator(List<string> validationSource, bool isStrict)
    {
        ValidationSource = validationSource;
        IsStrict = isStrict;
    }

    public bool IsValid(object val)
    {
        Console.WriteLine("Validating " + val);
        try
        {
            var valStr = val.ToString();
            Console.WriteLine("Comparing " + valStr + " to " + string.Join(",", ValidationSource));
            var isValid = ValidationSource.Any(x => x.CompareTo(valStr) == 0);
            Console.WriteLine(isValid);
            return isValid;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}