using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Clipboard;

/// <summary>
/// Reads the first table of an HTML clipboard fragment, as written by this library, Excel or Google Sheets,
/// into a snapshot. Tolerant of the loosely formed markup those produce.
/// </summary>
public static partial class ClipboardHtmlParser
{
    private static readonly Regex TableRegex = new(@"<table\b[^>]*>(.*?)(?:</table\s*>|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex RowRegex = new(@"<tr\b[^>]*>(.*?)(?=<tr\b|</table|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CellRegex = new(@"<(t[dh])\b([^>]*)>(.*?)(?=<t[dh]\b|</tr\b|$)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttributeRegex = new(@"([\w:.-]+)\s*(?:=\s*(?:""([^""]*)""|'([^']*)'|([^\s>]+)))?",
        RegexOptions.Compiled);

    private static readonly Regex StyleBlockRegex = new(@"<style\b[^>]*>(.*?)</style\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex ClassRuleRegex = new(@"([^{}]+)\{([^}]*)\}",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new(@"<[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex BreakRegex = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex ClipboardIdRegex = new(
        ClipboardHtmlWriter.ClipboardIdAttribute + @"\s*=\s*[""']?([0-9a-fA-F-]{32,36})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private const char LineBreakMarker = '\u0001';

    /// <summary>
    /// Finds the workbook clipboard id a BlazorDatasheet copy wrote into the HTML.
    /// </summary>
    public static bool TryGetClipboardId(string? html, out Guid id)
    {
        id = Guid.Empty;
        if (string.IsNullOrEmpty(html))
            return false;
        var match = ClipboardIdRegex.Match(html);
        return match.Success && Guid.TryParse(match.Groups[1].Value, out id);
    }

    /// <summary>
    /// Whether the HTML holds a table to paste.
    /// </summary>
    public static bool ContainsTable(string? html) => !string.IsNullOrEmpty(html) && TableRegex.IsMatch(html);

    /// <summary>
    /// Parses the first table in <paramref name="html"/>. Returns null when there is none.
    /// </summary>
    public static ClipboardSnapshot? Parse(string? html)
    {
        if (string.IsNullOrEmpty(html))
            return null;

        var table = TableRegex.Match(html);
        if (!table.Success)
            return null;

        var classRules = ParseClassRules(html);

        // first pass: lay the cells out, honouring row/col spans, to learn the size
        var placed = new List<(int row, int col, int rowSpan, int colSpan, string attributes, string content)>();
        var occupied = new HashSet<(int row, int col)>();
        var rowIndex = 0;
        foreach (Match row in RowRegex.Matches(table.Groups[1].Value))
        {
            var col = 0;
            foreach (Match cell in CellRegex.Matches(row.Groups[1].Value))
            {
                while (occupied.Contains((rowIndex, col)))
                    col++;
                var attributes = cell.Groups[2].Value;
                var attrs = ParseAttributes(attributes);
                var rowSpan = Math.Max(1, ParseInt(attrs.GetValueOrDefault("rowspan")));
                var colSpan = Math.Max(1, ParseInt(attrs.GetValueOrDefault("colspan")));
                placed.Add((rowIndex, col, rowSpan, colSpan, attributes, cell.Groups[3].Value));
                for (var r = rowIndex; r < rowIndex + rowSpan; r++)
                for (var c = col; c < col + colSpan; c++)
                    occupied.Add((r, c));
                col += colSpan;
            }

            rowIndex++;
        }

        if (placed.Count == 0)
            return null;

        var height = placed.Max(p => p.row + p.rowSpan);
        var width = placed.Max(p => p.col + p.colSpan);
        var snapshot = new ClipboardSnapshot(height, width);
        if (TryGetClipboardId(html, out var id))
            snapshot.SourceId = id;
        var tableAttrs = ParseAttributes(Regex.Match(table.Value, @"<table\b([^>]*)>", RegexOptions.IgnoreCase).Groups[1].Value);
        if (tableAttrs.TryGetValue(ClipboardHtmlWriter.OriginAttribute, out var originText))
        {
            var parts = originText.Split(',');
            if (parts.Length == 2 && int.TryParse(parts[0], out var originRow) && int.TryParse(parts[1], out var originCol))
                snapshot.Origin = new CellPosition(originRow, originCol);
        }

        foreach (var (r, c, rowSpan, colSpan, attributes, content) in placed)
        {
            if (rowSpan > 1 || colSpan > 1)
                snapshot.Merges.Add(new Region(r, r + rowSpan - 1, c, c + colSpan - 1));
            var cell = ParseCell(attributes, content, classRules);
            if (cell != null && !cell.IsEmpty)
                snapshot[r, c] = cell;
        }

        return snapshot;
    }

    private static ClipboardCell? ParseCell(string attributeText, string content,
        Dictionary<string, string> classRules)
    {
        var attrs = ParseAttributes(attributeText);
        var text = InnerText(content);

        var format = new CellFormat();
        var css = new List<string>();
        if (attrs.TryGetValue("class", out var classes))
            foreach (var name in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (classRules.TryGetValue(name, out var rule))
                    css.Add(rule);
        if (attrs.TryGetValue("style", out var style))
            css.Add(style);
        foreach (var declarations in css)
            ApplyCss(format, declarations);
        ApplyInlineTags(format, content);

        if (attrs.TryGetValue(ClipboardHtmlWriter.NumberFormatAttribute, out var numberFormat))
            format.NumberFormat = numberFormat;

        var value = CellValue.Empty;
        string? rawText = null;
        var formula = attrs.GetValueOrDefault(ClipboardHtmlWriter.FormulaAttribute);
        if (formula is { Length: > 0 } && !formula.StartsWith('='))
            formula = "=" + formula;

        if (ClipboardValueText.TryParseInvariant(attrs.GetValueOrDefault(ClipboardHtmlWriter.TypeAttribute),
                attrs.GetValueOrDefault(ClipboardHtmlWriter.ValueAttribute), out var typed))
            value = typed;
        else if (TryParseGoogleSheetsValue(attrs.GetValueOrDefault("data-sheets-value"), out var sheetsValue))
            value = sheetsValue;
        else if (attrs.TryGetValue("x:num", out var excelNumber) && excelNumber.Length > 0 &&
                 double.TryParse(excelNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            value = CellValue.Number(number);
        else if (text.Length > 0)
            rawText = text;

        var cellType = attrs.GetValueOrDefault(ClipboardHtmlWriter.CellTypeAttribute);

        return new ClipboardCell
        {
            Value = value,
            RawText = rawText,
            Formula = formula is { Length: > 1 } ? formula : null,
            Format = format.IsDefaultFormat() ? null : format,
            Type = string.IsNullOrEmpty(cellType) ? null : cellType
        };
    }

    private static bool TryParseGoogleSheetsValue(string? json, out CellValue value)
    {
        // {"1":3,"3":42} - key 2 is a string, 3 a number, 4 a boolean
        value = CellValue.Empty;
        if (string.IsNullOrEmpty(json))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return false;
            if (root.TryGetProperty("3", out var number) && number.ValueKind == JsonValueKind.Number)
            {
                value = CellValue.Number(number.GetDouble());
                return true;
            }

            if (root.TryGetProperty("4", out var logical) &&
                logical.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                value = CellValue.Logical(logical.GetBoolean());
                return true;
            }

            if (root.TryGetProperty("2", out var str) && str.ValueKind == JsonValueKind.String)
            {
                value = CellValue.Text(str.GetString() ?? string.Empty);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }

    private static string InnerText(string content)
    {
        var text = BreakRegex.Replace(content, LineBreakMarker.ToString());
        text = TagRegex.Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text).Replace(' ', ' ');
        text = WhitespaceRegex.Replace(text, " ").Trim();
        return text.Replace(LineBreakMarker.ToString(), "\n").Trim();
    }

    private static Dictionary<string, string> ParseAttributes(string text)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in AttributeRegex.Matches(text))
        {
            var name = m.Groups[1].Value;
            var value = m.Groups[2].Success ? m.Groups[2].Value :
                m.Groups[3].Success ? m.Groups[3].Value :
                m.Groups[4].Success ? m.Groups[4].Value : string.Empty;
            attrs[name] = WebUtility.HtmlDecode(value);
        }

        return attrs;
    }

    private static Dictionary<string, string> ParseClassRules(string html)
    {
        var rules = new Dictionary<string, string>();
        foreach (Match block in StyleBlockRegex.Matches(html))
        foreach (Match rule in ClassRuleRegex.Matches(block.Groups[1].Value))
        {
            var declarations = rule.Groups[2].Value;
            foreach (var selector in rule.Groups[1].Value.Split(','))
            {
                // ".xl65", "td.xl65" - keep the class part only
                var dot = selector.LastIndexOf('.');
                if (dot < 0)
                    continue;
                var name = selector[(dot + 1)..].Trim();
                if (name.Length == 0 || name.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-' && ch != '_'))
                    continue;
                rules[name] = rules.TryGetValue(name, out var existing) ? existing + ";" + declarations : declarations;
            }
        }

        return rules;
    }

    private static int ParseInt(string? text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;

    private static void ApplyInlineTags(CellFormat format, string content)
    {
        if (Regex.IsMatch(content, @"<(b|strong)\b", RegexOptions.IgnoreCase))
            format.FontWeight ??= "bold";
        if (Regex.IsMatch(content, @"<(i|em)\b", RegexOptions.IgnoreCase))
            format.FontStyle ??= "italic";
        if (Regex.IsMatch(content, @"<u\b", RegexOptions.IgnoreCase))
            format.TextDecoration ??= "underline";
        var font = Regex.Match(content, @"<font\b[^>]*\bcolor\s*=\s*[""']?([^""'\s>]+)", RegexOptions.IgnoreCase);
        if (font.Success)
            format.ForegroundColor ??= font.Groups[1].Value;
    }

    /// <summary>
    /// Applies CSS declarations ("prop:value;...") to a format. Unknown properties are ignored.
    /// </summary>
    public static void ApplyCss(CellFormat format, string declarations)
    {
        foreach (var declaration in declarations.Split(';'))
        {
            var colon = declaration.IndexOf(':');
            if (colon <= 0)
                continue;
            var property = declaration[..colon].Trim().ToLowerInvariant();
            var value = declaration[(colon + 1)..].Trim().Trim('"', '\'');
            if (value.Length == 0 || value is "inherit" or "initial" or "unset")
                continue;

            switch (property)
            {
                case "font-weight":
                    if (value is "bold" or "bolder" || (int.TryParse(value, out var weight) && weight >= 600))
                        format.FontWeight = "bold";
                    break;
                case "font-style":
                    if (value.StartsWith("italic") || value.StartsWith("oblique"))
                        format.FontStyle = "italic";
                    break;
                case "text-decoration":
                case "text-decoration-line":
                    if (value.Contains("underline"))
                        format.TextDecoration = "underline";
                    else if (value.Contains("line-through"))
                        format.TextDecoration = "line-through";
                    break;
                case "color":
                    format.ForegroundColor = Color(value);
                    break;
                case "background":
                case "background-color":
                    var color = Color(value.Split(' ')[0]);
                    if (color is not (null or "transparent" or "none"))
                        format.BackgroundColor = color;
                    break;
                case "text-align":
                    format.HorizontalTextAlign = value switch
                    {
                        "left" or "start" => TextAlign.Start,
                        "center" => TextAlign.Center,
                        "right" or "end" => TextAlign.End,
                        _ => format.HorizontalTextAlign
                    };
                    break;
                case "vertical-align":
                    format.VerticalTextAlign = value switch
                    {
                        "top" => TextAlign.Start,
                        "middle" => TextAlign.Center,
                        "bottom" => TextAlign.End,
                        _ => format.VerticalTextAlign
                    };
                    break;
                case "border":
                    var all = ParseBorder(value);
                    if (all != null)
                    {
                        format.BorderLeft = all.Clone();
                        format.BorderRight = all.Clone();
                        format.BorderTop = all.Clone();
                        format.BorderBottom = all.Clone();
                    }

                    break;
                case "border-left":
                    format.BorderLeft = ParseBorder(value) ?? format.BorderLeft;
                    break;
                case "border-right":
                    format.BorderRight = ParseBorder(value) ?? format.BorderRight;
                    break;
                case "border-top":
                    format.BorderTop = ParseBorder(value) ?? format.BorderTop;
                    break;
                case "border-bottom":
                    format.BorderBottom = ParseBorder(value) ?? format.BorderBottom;
                    break;
                case "white-space":
                    if (value is "normal" or "pre-wrap" or "pre-line")
                        format.TextWrap = TextWrapping.Wrap;
                    break;
                case "mso-number-format":
                    var numberFormat = value.Replace("\\", string.Empty);
                    if (numberFormat.Length > 0 && numberFormat != "General")
                        format.NumberFormat = numberFormat;
                    break;
            }
        }
    }

    private static string? Color(string value)
    {
        if (value.Length == 0)
            return null;
        return value.ToLowerInvariant() switch
        {
            "windowtext" => "#000000",
            "window" => "#ffffff",
            _ => value
        };
    }

    private static Border? ParseBorder(string value)
    {
        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0 || tokens.Any(t => t is "none" or "hidden"))
            return null;

        int? width = null;
        string? color = null;
        foreach (var token in tokens)
        {
            var lower = token.ToLowerInvariant();
            if (lower is "solid" or "dashed" or "dotted" or "double")
                continue;
            if (lower is "thin")
                width = 1;
            else if (lower is "medium")
                width = 2;
            else if (lower is "thick")
                width = 3;
            else if (char.IsDigit(lower[0]) || lower[0] == '.')
            {
                var unit = lower.TrimStart("0123456789.".ToCharArray());
                var numberText = lower[..^unit.Length];
                if (double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    if (unit == "pt")
                        number *= 4.0 / 3.0;
                    width = Math.Max(1, (int)Math.Round(number));
                }
            }
            else
                color = Color(token);
        }

        if (width is null or <= 0)
            width = 1;
        return new Border { Width = width, Color = color ?? "#000000" };
    }
}
