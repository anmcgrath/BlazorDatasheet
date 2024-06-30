using System.Text;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Render;

public class VisualCell
{
    public object? Value { get; private set; }
    public string FormattedString { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }
    public IRegion? Merge { get; private set; }
    public double X { get; private set; }
    public double Y { get; private set; }
    public string CellType { get; private set; } = "default";
    public string FormatStyleString { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public CellFormat? Format { get; private set; }
    
    public bool IsVisible { get; set; }

    public VisualCell(int row, int col, Sheet sheet)
    {
        Merge = sheet.Cells.GetMerge(row, col)?.GetIntersection(sheet.Region);

        var cell = sheet.Cells.GetCell(row, col);
        var format = cell.Format.Clone();

        var cellValue = sheet.Cells.GetCellValue(row, col);
        Value = cellValue.Data;
        
        if (cellValue.ValueType == CellValueType.Number && format.NumberFormat != null)
            FormattedString = (cellValue.GetValue<double>()).ToString(format.NumberFormat);
        else if (cellValue.ValueType == CellValueType.Date && format.NumberFormat != null)
            FormattedString = (cellValue.GetValue<DateTime>()).ToString(format.NumberFormat);
        else
            FormattedString = Value?.ToString() ?? string.Empty;

        var cf = sheet.ConditionalFormats.GetFormatResult(row, col);
        if (cf != null)
            format.Merge(cf);
        
        Row = row;
        Col = col;

        IsVisible = cell.IsVisible;

        X = sheet.Columns.GetVisualTop(col);
        Y = sheet.Rows.GetVisualTop(row);
        
        FormatStyleString = GetCellFormatStyleString(Row, Col, format, cell.IsValid, cellValue.ValueType);
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
            FormatStyleString = GetCellFormatStyleString(row, col, defaultFormat, true, CellValueType.Text),
            X = sheet.Columns.GetVisualTop(col),
            Y = sheet.Rows.GetVisualTop(row),
            CellType = "default",
            Format = defaultFormat
        };
    }

    private static string GetCellFormatStyleString(int row, int col, CellFormat? format, bool isCellValid,
        CellValueType type)
    {
        if (format == null)
            return string.Empty;

        var sb = new StyleBuilder();

        if (!isCellValid)
            sb.AddStyle("color", "var(--invalid-cell-foreground-color)");
        else
            sb.AddStyle("color", format.ForegroundColor!, format.ForegroundColor != null);

        sb.AddStyle("background-color", format.BackgroundColor!, format.BackgroundColor != null);
        sb.AddStyle("font-weight", format.FontWeight!, format.FontWeight != null);

        if (format.BorderBottom != null)
            sb.AddStyle("border-bottom", $"{format.BorderBottom.Width}px solid {format.BorderBottom.Color};");
        if (format.BorderRight != null)
            sb.AddStyle("border-right", $"{format.BorderRight.Width}px solid {format.BorderRight.Color};");

        // if number and no align is set, move to right
        if (type == CellValueType.Number && format.TextAlign == null)
            sb.AddStyle("text-align", "right");
        else
            sb.AddStyleNotNull("text-align", format.TextAlign);

        return sb.ToString();
    }
}