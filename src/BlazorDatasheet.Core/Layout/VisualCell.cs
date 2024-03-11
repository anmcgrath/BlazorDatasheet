using System.Text;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;

namespace BlazorDatasheet.Core.Layout;

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
        FormatStyleString = GetCellFormatStyleString(Row, Col, format, cell.IsValid);
        Icon = format?.Icon;
        CellType = cell.Type;
        Format = format;
    }

    private VisualCell()
    {
    }

    public static VisualCell Empty(int row, int col, Sheet sheet, ref CellFormat defaultFormat)
    {
        return new VisualCell()
        {
            Row = row,
            Col = col,
            FormatStyleString = GetCellFormatStyleString(row, col, defaultFormat, true),
            Width = sheet.Columns.GetWidth(col),
            Height = sheet.Rows.GetHeight(row),
            X = sheet.Columns.GetLeft(col),
            Y = sheet.Rows.GetTop(row),
            CellType = "text",
            Format = defaultFormat
        };
    }

    private static string GetCellFormatStyleString(int row, int col, CellFormat? format, bool isCellValid)
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
            if (format?.BorderBottom == null)
                sb.Append($"border-bottom: var(--sheet-border-style);");
            if (format?.BorderRight == null)
                sb.Append($"border-right: var(--sheet-border-style);" +
                          $"" +
                          $"" +
                          $"");
        }

        if (format?.BorderBottom != null)
            sb.Append($"border-bottom: {format.BorderBottom.Width}px solid {format.BorderBottom.Color};");
        if (format?.BorderRight != null)
            sb.Append($"border-right: {format.BorderRight.Width}px solid {format.BorderRight.Color};");
        if (format?.BorderLeft != null && col == 0)
            sb.Append($"border-left: {format.BorderLeft.Width}px solid {format.BorderLeft.Color};");
        if (format?.BorderTop != null && row == 0)
            sb.Append($"border-top: {format.BorderTop.Width}px solid {format.BorderTop.Color};");
        
        if (!string.IsNullOrWhiteSpace(format?.TextAlign))
        {
            sb.Append($"text-align: {format.TextAlign};");
        }

        return sb.ToString();
    }
}