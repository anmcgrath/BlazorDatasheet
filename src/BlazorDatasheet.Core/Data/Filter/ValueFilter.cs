using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class ValueFilter : IFilter
{
    private HashSet<CellValue> _values = new();

    public ValueFilter(List<CellValue> values)
    {
        _values = values.ToHashSet();
    }

    public ValueFilter(HashSet<CellValue> values)
    {
        _values = values;
    }

    public void Include(CellValue value)
    {
        _values.Add(value);
    }

    public void Exclude(CellValue value)
    {
        _values.Remove(value);
    }

    public bool Includes(CellValue value)
    {
        return IncludeAll || _values.Contains(value);
    }

    public bool Match(CellValue cellValue)
    {
        return Includes(cellValue);
    }

    public bool IncludeBlanks { get; set; }
    public bool IncludeAll { get; set; }
}