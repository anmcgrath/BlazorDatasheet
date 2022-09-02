using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Validation;

public class SourceValidator<T> : IDataValidator where T : IComparable
{
    public List<T> ValidationSource { get; }
    public bool IsStrict { get; private set; }

    public SourceValidator(List<T> validationSource, bool isStrict)
    {
        ValidationSource = validationSource;
        IsStrict = isStrict;
    }

    public bool IsValid(IReadOnlyCell cell)
    {
        try
        {
            var val = cell.GetValue<T>();
            return ValidationSource.Any(x => x.CompareTo(val) == 0);
        }
        catch (Exception e)
        {
            return false;
        }
    }
}