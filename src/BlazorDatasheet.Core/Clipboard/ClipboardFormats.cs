using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Clipboard;

internal static class ClipboardFormats
{
    /// <summary>
    /// The part of a cell's visible format that travels with a copy: appearance only, never locks, css hooks
    /// or icons. Returns null when nothing is left.
    /// </summary>
    public static CellFormat? Portable(CellFormat? format)
    {
        if (format == null || format.IsDefaultFormat())
            return null;

        var portable = new CellFormat();
        if (format.FontWeight != null) portable.FontWeight = format.FontWeight;
        if (format.FontStyle != null) portable.FontStyle = format.FontStyle;
        if (format.TextDecoration != null) portable.TextDecoration = format.TextDecoration;
        if (format.ForegroundColor != null) portable.ForegroundColor = format.ForegroundColor;
        if (format.BackgroundColor != null) portable.BackgroundColor = format.BackgroundColor;
        if (format.NumberFormat != null) portable.NumberFormat = format.NumberFormat;
        if (format.HorizontalTextAlign != null) portable.HorizontalTextAlign = format.HorizontalTextAlign;
        if (format.VerticalTextAlign != null) portable.VerticalTextAlign = format.VerticalTextAlign;
        if (format.BorderLeft != null) portable.BorderLeft = format.BorderLeft.Clone();
        if (format.BorderRight != null) portable.BorderRight = format.BorderRight.Clone();
        if (format.BorderTop != null) portable.BorderTop = format.BorderTop.Clone();
        if (format.BorderBottom != null) portable.BorderBottom = format.BorderBottom.Clone();
        if (format.TextWrap == TextWrapping.Wrap) portable.TextWrap = TextWrapping.Wrap;
        if (format.BackgroundPattern != null) portable.BackgroundPattern = format.BackgroundPattern;

        return portable.IsDefaultFormat() ? null : portable;
    }
}
