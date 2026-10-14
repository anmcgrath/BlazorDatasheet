using System.Net;
using System.Text;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Clipboard;

/// <summary>
/// Writes a snapshot as an HTML table that Excel and Google Sheets paste with formatting, and that carries
/// enough extra data for another BlazorDatasheet to paste it exactly.
/// </summary>
public static class ClipboardHtmlWriter
{
    public const string ClipboardIdAttribute = "data-bds-clipboard-id";
    public const string FormulaAttribute = "data-bds-formula";
    public const string TypeAttribute = "data-bds-type";
    public const string ValueAttribute = "data-bds-value";
    public const string CellTypeAttribute = "data-bds-cell-type";
    public const string NumberFormatAttribute = "data-bds-number-format";
    public const string OriginAttribute = "data-bds-origin";
    public const string SpillAnchorAttribute = "data-bds-spill-anchor";

    public static string Write(ClipboardSnapshot snapshot)
    {
        var covered = new HashSet<(int row, int col)>();
        var anchors = new Dictionary<(int row, int col), IRegion>();
        foreach (var merge in snapshot.Merges)
        {
            anchors[(merge.Top, merge.Left)] = merge;
            for (var r = merge.Top; r <= merge.Bottom; r++)
            for (var c = merge.Left; c <= merge.Right; c++)
                if (r != merge.Top || c != merge.Left)
                    covered.Add((r, c));
        }

        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset=\"utf-8\"></head><body><!--StartFragment-->");
        sb.Append("<table ").Append(ClipboardIdAttribute).Append("=\"")
            .Append(snapshot.SourceId?.ToString("N") ?? string.Empty)
            .Append("\" data-bds-version=\"1\"");
        if (snapshot.Origin is { } origin)
            sb.Append(' ').Append(OriginAttribute).Append("=\"").Append(origin.row).Append(',').Append(origin.col).Append('"');
        sb.Append(" style=\"border-collapse:collapse\"><tbody>");

        for (var r = 0; r < snapshot.Height; r++)
        {
            sb.Append("<tr>");
            for (var c = 0; c < snapshot.Width; c++)
            {
                if (covered.Contains((r, c)))
                    continue;
                var cell = snapshot[r, c];
                sb.Append("<td");
                if (anchors.TryGetValue((r, c), out var merge))
                {
                    if (merge.Height > 1) sb.Append(" rowspan=\"").Append(merge.Height).Append('"');
                    if (merge.Width > 1) sb.Append(" colspan=\"").Append(merge.Width).Append('"');
                }

                if (cell != null)
                {
                    var css = CssFor(cell.Format);
                    if (css.Length > 0)
                        Attribute(sb, "style", css);
                    if (cell.Formula != null)
                        Attribute(sb, FormulaAttribute, cell.Formula);
                    if (cell.SpillAnchor is { } spillAnchor)
                        Attribute(sb, SpillAnchorAttribute, $"{spillAnchor.row},{spillAnchor.col}");
                    if (!cell.Value.IsEmpty && ClipboardValueText.Invariant(cell.Value) is { } invariant)
                    {
                        Attribute(sb, TypeAttribute, ClipboardValueText.TypeName(cell.Value));
                        Attribute(sb, ValueAttribute, invariant);
                    }

                    if (cell.Type != null)
                        Attribute(sb, CellTypeAttribute, cell.Type);
                    if (cell.Format?.NumberFormat != null)
                        Attribute(sb, NumberFormatAttribute, cell.Format.NumberFormat);
                }

                sb.Append('>');
                if (cell != null)
                {
                    var text = cell.RawText ?? ClipboardValueText.Display(cell.Value);
                    sb.Append(WebUtility.HtmlEncode(text).Replace("\r\n", "<br>").Replace("\n", "<br>"));
                }

                sb.Append("</td>");
            }

            sb.Append("</tr>");
        }

        sb.Append("</tbody></table><!--EndFragment--></body></html>");
        return sb.ToString();
    }

    private static void Attribute(StringBuilder sb, string name, string value)
    {
        sb.Append(' ').Append(name).Append("=\"").Append(WebUtility.HtmlEncode(value)).Append('"');
    }

    /// <summary>
    /// The inline CSS for a cell format.
    /// </summary>
    public static string CssFor(CellFormat? format)
    {
        if (format == null || format.IsDefaultFormat())
            return string.Empty;

        var sb = new StringBuilder();
        void Add(string property, string? value)
        {
            if (string.IsNullOrEmpty(value))
                return;
            sb.Append(property).Append(':').Append(value).Append(';');
        }

        Add("font-weight", format.FontWeight);
        Add("font-style", format.FontStyle);
        Add("text-decoration", format.TextDecoration);
        Add("color", format.ForegroundColor);
        Add("background-color", format.BackgroundColor);
        Add("text-align", format.HorizontalTextAlign switch
        {
            TextAlign.Start => "left",
            TextAlign.Center => "center",
            TextAlign.End => "right",
            _ => null
        });
        Add("vertical-align", format.VerticalTextAlign switch
        {
            TextAlign.Start => "top",
            TextAlign.Center => "middle",
            TextAlign.End => "bottom",
            _ => null
        });
        Add("border-left", BorderCss(format.BorderLeft));
        Add("border-right", BorderCss(format.BorderRight));
        Add("border-top", BorderCss(format.BorderTop));
        Add("border-bottom", BorderCss(format.BorderBottom));
        if (format.TextWrap == TextWrapping.Wrap)
            Add("white-space", "normal");
        return sb.ToString();
    }

    private static string? BorderCss(Border? border)
    {
        if (border == null || border.Width is null or <= 0)
            return null;
        return $"{border.Width}px solid {border.Color ?? "#000000"}";
    }
}
