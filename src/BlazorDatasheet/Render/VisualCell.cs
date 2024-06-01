using System.Text;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Render;

public class VisualCell
{
    public object? Value { get; private set; }
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool Visible { get; private set; } = true;
    public int ColSpan { get; private set; } = 1;
    public int RowSpan { get; private set; } = 1;
    public double X { get; private set; }
    public double Y { get; private set; }
    public string CellType { get; private set; }
    public string FormatStyleString { get; private set; }
    public string? Icon { get; private set; }
    public CellFormat? Format { get; private set; }

    public VisualCell(int row, int col, Sheet sheet)
    {
        var merge = sheet.Cells.GetMerge(row, col);
        if (merge != null)
        {
            if (merge.Top == row && merge.Left == col)
            {
                ColSpan = merge.Width;
                RowSpan = merge.Height;
            }
            else
            {
                Visible = false;
                return;
            }
        }

        var cell = sheet.Cells.GetCell(row, col);
        var format = cell.Format.Clone();
        Value = cell.Value;
        var cf = sheet.ConditionalFormats.GetFormatResult(row, col);
        if (cf != null)
            format.Merge(cf);
        Row = row;
        Col = col;

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
            X = sheet.Columns.GetLeft(col),
            Y = sheet.Rows.GetTop(row),
            CellType = "default",
            Format = defaultFormat
        };
    }

    private static string GetCellFormatStyleString(int row, int col, CellFormat? format, bool isCellValid)
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
        if (format.BorderLeft != null)
            sb.AddStyle("border-left", "${format.BorderLeft.Width}px solid {format.BorderLeft.Color};");
        if (format.BorderTop != null)
            sb.AddStyle("border-top", $"{format.BorderTop.Width}px solid {format.BorderTop.Color};");

        sb.AddStyle("text-align", format.TextAlign!, format.TextAlign != null);

        return sb.ToString();
    }
}