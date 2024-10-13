using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Filter;

public class ValueFilter : IFilter
{
    private HashSet<CellValue> _excluded = new();

    public ValueFilter()
    {
        _excluded = new();
    }

    public void Include(CellValue value)
    {
        _excluded.Remove(value);
        IncludeAll = _excluded.Count == 0;
    }

    public void Exclude(CellValue value)
    {
        _excluded.Add(value);
        IncludeAll = _excluded.Count == 0;
    }

    public bool Includes(CellValue value)
    {
        return IncludeAll || !_excluded.Contains(value);
    }

    public bool Match(CellValue cellValue)
    {
        return !_excluded.Contains(cellValue);
    }

    public bool IncludeBlanks { get; set; } = true;

    public IFilter Clone()
    {
        return new ValueFilter()
        {
            _includeAll = IncludeAll,
            IncludeBlanks = IncludeBlanks,
            _excluded = _excluded.ToHashSet()
        };
    }

    private bool _includeAll = true;

    public bool IncludeAll
    {
        get => _includeAll;
        set
        {
            if (value)
                _excluded.Clear();

            IncludeBlanks = value;
            _includeAll = value;
        }
    }
}