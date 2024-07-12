using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class ValueFilter : IColumnFilter
{
    private HashSet<CellValue> _values = new();

    public void SetValues(List<CellValue> values)
    {
        _values = values.ToHashSet();
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
        return _values.Contains(value);
    }

    public bool Match(CellValue cellValue)
    {
        return Includes(cellValue);
    }

    public bool IncludeBlanks { get; set; }
}