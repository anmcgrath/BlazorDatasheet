using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Metadata;

public class CellMetadata : IMergeable<CellMetadata>, IEquatable<CellMetadata>
{
    private Dictionary<string, object>? _data;

    public void Merge(CellMetadata item)
    {
        if (item._data == null)
            return;

        if (_data == null)
            _data = item._data.ToDictionary(x => x.Key, x => x.Value);
        else
            item._data.ToList().ForEach(x => _data[x.Key] = x.Value);
    }

    public CellMetadata Clone()
    {
        return new CellMetadata()
        {
            _data = _data?.ToDictionary(x => x.Key, y => y.Value)
        };
    }

    public void SetItem(string key, object item)
    {
        if (_data == null)
            _data = new();
        if (!_data.TryAdd(key, item))
            _data[key] = item;
    }

    public object? GetItem(string key)
    {
        if (_data != null && _data.TryGetValue(key, out var val))
            return val;
        return null;
    }

    public bool Equals(CellMetadata? other)
    {
        if (other == null)
            return false;

        if (other._data == null && _data == null)
            return true;

        if (other._data == null || _data == null)
            return false;

        return _data.OrderBy(x => x.Key)
            .SequenceEqual(other._data.OrderBy(x => x.Key));
    }
}