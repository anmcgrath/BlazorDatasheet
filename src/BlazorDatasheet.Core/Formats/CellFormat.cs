using BlazorDatasheet.DataStructures.Intervals;

namespace BlazorDatasheet.Core.Formats;

public class CellFormat : IMergeable<CellFormat>, IEquatable<CellFormat>, IReadonlyCellFormat
{
    private Dictionary<string, object?>? _styles;
    internal IReadOnlyDictionary<string, object?>? Styles => _styles;

    public CellFormat()
    {
    }

    internal CellFormat(Dictionary<string, object?> styles)
    {
        _styles = styles;
    }

    /// <summary>
    /// CSS font-weight
    /// </summary>
    public string? FontWeight
    {
        get => GetStyleOrDefault<string>(nameof(FontWeight));
        set => AddStyle(nameof(FontWeight), value);
    }

    /// <summary>
    /// CSS background color
    /// </summary>
    public string? BackgroundColor
    {
        get => GetStyleOrDefault<string>(nameof(BackgroundColor));
        set => AddStyle(nameof(BackgroundColor), value);
    }

    /// <summary>
    /// CSS color
    /// </summary>
    public string? ForegroundColor
    {
        get => GetStyleOrDefault<string>(nameof(ForegroundColor));
        set => AddStyle(nameof(ForegroundColor), value);
    }

    /// <summary>
    /// How to format the string when rendered.
    /// </summary>
    public string? NumberFormat
    {
        get => GetStyleOrDefault<string>(nameof(NumberFormat));
        set => AddStyle(nameof(NumberFormat), value);
    }

    /// <summary>
    /// The name of displayed inside the cell
    /// </summary>
    public string? Icon
    {
        get => GetStyleOrDefault<string>(nameof(Icon));
        set => AddStyle(nameof(Icon), value);
    }

    /// <summary>
    /// The icon's CSS color
    /// </summary>
    public string? IconColor
    {
        get => GetStyleOrDefault<string>(nameof(IconColor));
        set => AddStyle(nameof(IconColor), value);
    }

    /// <summary>
    /// Whether the cell's value can be modified by the user.
    /// </summary>
    public bool? IsReadOnly
    {
        get => GetStyleOrDefault<bool?>(nameof(IsReadOnly));
        set => AddStyle(nameof(IsReadOnly), value);
    }

    /// <summary>
    /// Horizontal text align.
    /// </summary>
    public TextAlign? HorizontalTextAlign
    {
        get => GetStyleOrDefault<TextAlign?>(nameof(HorizontalTextAlign));
        set => AddStyle(nameof(HorizontalTextAlign), value);
    }

    /// <summary>
    /// Vertical text align.
    /// </summary>
    public TextAlign? VerticalTextAlign
    {
        get => GetStyleOrDefault<TextAlign?>(nameof(VerticalTextAlign));
        set => AddStyle(nameof(VerticalTextAlign), value);
    }

    /// <summary>
    /// Left border
    /// </summary>
    public Border? BorderLeft
    {
        get => GetStyleOrDefault<Border?>(nameof(BorderLeft));
        set => AddStyle(nameof(BorderLeft), value);
    }

    /// <summary>
    /// Right border
    /// </summary>
    public Border? BorderRight
    {
        get => GetStyleOrDefault<Border?>(nameof(BorderRight));
        set => AddStyle(nameof(BorderRight), value);
    }

    /// <summary>
    /// Top border
    /// </summary>
    public Border? BorderTop
    {
        get => GetStyleOrDefault<Border?>(nameof(BorderTop));
        set => AddStyle(nameof(BorderTop), value);
    }

    /// <summary>
    /// Bottom border
    /// </summary>
    public Border? BorderBottom
    {
        get => GetStyleOrDefault<Border?>(nameof(BorderBottom));
        set => AddStyle<Border?>(nameof(BorderBottom), value);
    }

    public TextWrapping TextWrap
    {
        get => GetStyleOrDefault<TextWrapping>(nameof(TextWrapping));
        set => AddStyle(nameof(TextWrapping), value);
    }

    private void AddStyle<T>(string key, T? value)
    {
        _styles ??= new Dictionary<string, object?>();
        if (!_styles.TryAdd(key, value))
            _styles[key] = value;
    }

    private T? GetStyleOrDefault<T>(string key)
    {
        if (IsDefaultFormat())
            return default;

        return _styles!.TryGetValue(key, out var val) ? (T?)val : default;
    }

    private object? GetStyleOrDefault(string key)
    {
        if (IsDefaultFormat())
            return default;

        return _styles!.GetValueOrDefault(key);
    }

    public bool IsDefaultFormat() => _styles == null || _styles.Count == 0;

    /// <summary>
    /// Returns a new Format object with cloned properties
    /// </summary>
    /// <returns></returns>
    public CellFormat Clone()
    {
        if (_styles == null)
            return new CellFormat();

        var cfClone = new CellFormat();

        foreach (var kvp in _styles)
        {
            cfClone.AddStyle(kvp.Key, kvp.Value);
        }

        return cfClone;
    }

    /// <summary>
    /// Override this format's properties from a format object. This method only overrides if the properties exist on
    /// the overriding format object.
    /// </summary>
    /// <param name="format">The format object that will override properties of this object, if they exist.</param>
    public void Merge(CellFormat? format)
    {
        if (format?._styles == null)
            return;

        _styles ??= new Dictionary<string, object?>();
        foreach (var style in format._styles)
        {
            if (!_styles.TryAdd(style.Key, style.Value))
                _styles[style.Key] = style.Value;
        }

        MergeBorders(format);
    }

    private void MergeBorders(CellFormat format)
    {
        if (format.BorderBottom != null)
        {
            if (this.BorderBottom == null)
                this.BorderBottom = format.BorderBottom.Clone();
            else
                this.BorderBottom?.Merge(format.BorderBottom);
        }

        if (format.BorderLeft != null)
        {
            if (this.BorderLeft == null)
                this.BorderLeft = format.BorderLeft.Clone();
            else
                this.BorderLeft?.Merge(format.BorderLeft);
        }

        if (format.BorderRight != null)
        {
            if (this.BorderRight == null)
                this.BorderRight = format.BorderRight.Clone();
            else
                this.BorderRight?.Merge(format.BorderRight);
        }

        if (format.BorderTop != null)
        {
            if (this.BorderTop == null)
                this.BorderTop = format.BorderTop.Clone();
            else
                this.BorderTop?.Merge(format.BorderTop);
        }
    }

    public bool HasBorder() => BorderTop != null &&
                               BorderBottom != null &&
                               BorderRight != null &&
                               BorderLeft != null;


    public bool Equals(CellFormat? other)
    {
        if (other == null)
            return false;

        if (_styles == null && other._styles == null)
            return true;

        if (other._styles?.Count != _styles?.Count)
            return false;

        _styles ??= new Dictionary<string, object?>();

        foreach (var kp in _styles)
        {
            if (other.GetStyleOrDefault(kp.Key)?.Equals(GetStyleOrDefault(kp.Key)) == false)
                return false;
        }

        return true;
    }
}