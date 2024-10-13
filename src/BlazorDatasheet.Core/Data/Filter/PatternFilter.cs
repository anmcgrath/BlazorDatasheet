using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class PatternFilter : IFilter
{
    public PatternFilterType Type { get; set; }
    public string Value { get; set; }

    public PatternFilter()
    {
        Type = PatternFilterType.None;
        Value = string.Empty;
    }

    public PatternFilter(PatternFilterType type, string value)
    {
        Type = type;
        Value = value;
    }

    public bool Match(CellValue cellValue)
    {
        if (string.IsNullOrEmpty(Value))
            return true;

        if (Type == PatternFilterType.None)
            return true;

        if (cellValue.Data?.ToString() == null)
            return false;

        switch (Type)
        {
            case PatternFilterType.StartsWith:
                return cellValue.Data.ToString()!.StartsWith(Value);
            case PatternFilterType.EndsWith:
                return cellValue.Data.ToString()!.EndsWith(Value);
            case PatternFilterType.Contains:
                return cellValue.Data.ToString()!.Contains(Value);
            case PatternFilterType.NotStartsWith:
                return !cellValue.Data.ToString()!.StartsWith(Value);
            case PatternFilterType.NotEndsWith:
                return !cellValue.Data.ToString()!.EndsWith(Value);
            case PatternFilterType.NotContains:
                return !cellValue.Data.ToString()!.Contains(Value);
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool IncludeBlanks => Type == PatternFilterType.None || string.IsNullOrEmpty(Value);

    public IFilter Clone()
    {
        return new PatternFilter(this.Type, this.Value);
    }
}

public enum PatternFilterType
{
    None,
    StartsWith,
    EndsWith,
    Contains,
    NotStartsWith,
    NotEndsWith,
    NotContains
}