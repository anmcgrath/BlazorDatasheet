using System.Text;

namespace BlazorDatasheet.Util;

public class StyleBuilder
{
    /// <summary>
    /// Created on the first style actually written. A StyleBuilder is created for every cell that
    /// renders, and plenty of those end up with no styles at all.
    /// </summary>
    private StringBuilder? _stringBuilder;

    public StyleBuilder AddStyle(string name, string value, bool check)
    {
        if (check)
            Append(name, value);
        return this;
    }

    public StyleBuilder AddStyleNotNull(string name, string? value)
    {
        if (value != null)
            Append(name, value);
        return this;
    }

    public StyleBuilder AddStyle(string name, string value)
    {
        Append(name, value);
        return this;
    }

    private void Append(string name, string value)
    {
        _stringBuilder ??= new StringBuilder();
        _stringBuilder.Append(name).Append(": ").Append(value).Append(';');
    }

    public override string ToString()
    {
        return _stringBuilder?.ToString() ?? string.Empty;
    }
}
