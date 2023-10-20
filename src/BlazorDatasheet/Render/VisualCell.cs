using System.Text;
using BlazorDatasheet.Data;
using BlazorDatasheet.Formats;
using Microsoft.AspNetCore.Components;

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
    public RenderFragment? Icon { get; private set; }
    public CellFormat? Format { get; private set; }

    public VisualCell(int row, int col, Sheet sheet)
    {
        var cell = sheet.GetCell(row, col);
        var format = sheet.GetFormat(cell) ?? new CellFormat();
        Value = cell.GetValue();
        var cf = sheet.ConditionalFormatting.GetFormatResult(row, col);
        if (cf != null)
            format.Merge(cf);
        Row = row;
        Col = col;
        Width = sheet.ColumnInfo.GetWidth(col);
        Height = sheet.RowInfo.GetHeight(row);
        X = sheet.ColumnInfo.GetLeft(col);
        Y = sheet.RowInfo.GetTop(row);
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
            Width = sheet.ColumnInfo.GetWidth(col),
            Height = sheet.RowInfo.GetHeight(row),
            X = sheet.ColumnInfo.GetLeft(col),
            Y = sheet.RowInfo.GetTop(row),
            CellType = "text",
            Format = defaultFormat
        };
    }

    private static string GetCellFormatStyleString(CellFormat? format, bool isCellValid)
    {
        if (format == null)
            return string.Empty;

        var foreGroundVar = isCellValid ? "--sheet-foreground-color" : "--invalid-cell-foreground-color";
        var sb = new StringBuilder();
        sb.Append($"background-color:{format?.BackgroundColor ?? "var(--sheet-bg-color)"};");
        sb.Append($"color:{format?.ForegroundColor ?? $"var({foreGroundVar})"};");
        sb.Append($"font-weight:{format?.FontWeight ?? "var(--sheet-font-weight)"};");

        if (!string.IsNullOrWhiteSpace(format?.TextAlign))
        {
            sb.Append($"text-align: {format.TextAlign};");
        }

        return sb.ToString();
    }
}