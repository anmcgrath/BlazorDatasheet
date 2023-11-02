using System.Text;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using Microsoft.Extensions.Primitives;

namespace BlazorDatasheet.Render;

public class VisualCell
{
    public object? Value { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Width { get; private set; }
    public double Height { get; private set; }
    public string CellType { get; private set; }
    public string FormatStyleString { get; private set; }
    public string? Icon { get; private set; }
    public CellFormat? Format { get; private set; }

    public VisualCell(int row, int col, Sheet sheet)
    {
        var cell = sheet.Cells.GetCell(row, col);
        var format = cell.Format.Clone();
        Value = cell.Value;
        var cf = sheet.ConditionalFormats.GetFormatResult(row, col);
        if (cf != null)
            format.Merge(cf);
        Row = row;
        Col = col;

        Width = sheet.Columns.GetWidth(col);
        Height = sheet.Rows.GetHeight(row);

        X = sheet.Columns.GetLeft(col);
        Y = sheet.Rows.GetTop(row);
        FormatStyleString = GetCellFormatStyleString(format, cell.IsValid);
        Icon = format?.Icon;
        CellType = cell.Type;
        Format = format;
    }

    internal VisualCell()
    {
    }

    public static VisualCell Empty(int row, int col, Sheet sheet, ref CellFormat defaultFormat)
    {
        return new VisualCell()
        {
            Row = row,
            Col = col,
            FormatStyleString = GetCellFormatStyleString(defaultFormat, true),
            Width = sheet.Columns.GetWidth(col),
            Height = sheet.Rows.GetHeight(row),
            X = sheet.Columns.GetLeft(col),
            Y = sheet.Rows.GetTop(row),
            CellType = "text",
            Format = defaultFormat
        };
    }

    private static string GetCellFormatStyleString(CellFormat? format, bool isCellValid)
    {
        if (format == null)
            return string.Empty;

        var foregroundColor = isCellValid
            ? (format?.ForegroundColor ?? "var(--sheet-foreground-color)")
            : ("var(--invalid-cell-foreground-color)");
        var sb = new StringBuilder();
        sb.Append($"background-color:{format?.BackgroundColor ?? "var(--sheet-bg-color)"};");
        sb.Append($"color:{foregroundColor};");
        sb.Append($"font-weight:{format?.FontWeight ?? "var(--sheet-font-weight)"};");

        if (format?.BackgroundColor == null)
        {
            sb.Append($"border-right: var(--sheet-border-style);");
            sb.Append($"border-bottom: var(--sheet-border-style);");
        }

        if (!string.IsNullOrWhiteSpace(format?.TextAlign))
        {
            sb.Append($"text-align: {format.TextAlign};");
        }

        return sb.ToString();
    }
}