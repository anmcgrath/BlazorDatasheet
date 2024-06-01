using System.Text;

namespace BlazorDatasheet.Util;

public class StyleBuilder
{
    private StringBuilder _stringBuilder;

    public StyleBuilder()
    {
        _stringBuilder = new StringBuilder();
    }

    public StyleBuilder AddStyle(string name, string value, bool check)
    {
        if (check)
            _stringBuilder.Append($"{name}:{value};");
        return this;
    }

    public StyleBuilder AddStyleNotNull(string name, string? value)
    {
        if (value != null)
            _stringBuilder.Append($"{name}:{value};");
        return this;
    }

    public StyleBuilder AddStyle(string name, string value)
    {
        _stringBuilder.Append($"{name}:{value};");
        return this;
    }

    public override string ToString()
    {
        return _stringBuilder.ToString();
    }
}