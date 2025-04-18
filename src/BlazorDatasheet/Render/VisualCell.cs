using System.Diagnostics;
using System.Globalization;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Render;

public class VisualCell
{
    public object? Value { get; private set; }
    public string FormattedString { get; private set; } = string.Empty;
    public int Row { get; private set; }
    public int Col { get; private set; }
    public IRegion? Merge { get; private set; }
    public string CellType { get; private set; } = "default";
    public string FormatStyleString { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public CellFormat? Format { get; private set; }
    public bool IsVisible { get; set; }
    public int VisibleRowSpan { get; set; } = 1;
    public int VisibleColSpan { get; set; } = 1;
    public bool IsMergeStart { get; set; }
    public int VisibleMergeRowStart { get; set; }
    public int VisibleMergeColStart { get; set; }
    public double Height { get; set; }
    public double Width { get; set; }


    /// <summary>
    /// Create a visual cell, which has formatting properties calculated for the cell.
    /// </summary>
    /// <param name="row">The row of the cell</param>
    /// <param name="col">The column of the cell</param>
    /// <param name="sheet">The sheet that the cell is inside.</param>
    /// <param name="numberOfSignificantDigits">The number of digits to round the displayed number to.</param>
    internal VisualCell(int row, int col, Sheet sheet, int numberOfSignificantDigits)
    {
        Merge = sheet.Cells.GetMerge(row, col)?.GetIntersection(sheet.Region);

        if (Merge != null)
        {
            VisibleMergeColStart = sheet.Columns.GetNextVisible(Merge.Left - 1);
            VisibleMergeRowStart = sheet.Rows.GetNextVisible(Merge.Top - 1);

            IsMergeStart = row == VisibleMergeRowStart && col == VisibleMergeColStart;

            VisibleRowSpan = sheet.Rows.CountVisible(VisibleMergeRowStart, Merge.Bottom);
            VisibleColSpan = sheet.Columns.CountVisible(VisibleMergeColStart, Merge.Right);
        }

        var cell = sheet.Cells.GetCell(row, col);
        var format = cell.Format.Clone();

        var cellValue = sheet.Cells.GetCellValue(row, col);
        Value = cellValue.Data;

        if (cellValue.ValueType == CellValueType.Number)
        {
            var roundedNumber = Math.Round(cellValue.GetValue<double>(), numberOfSignificantDigits);
            if (format.NumberFormat != null)
                FormattedString = roundedNumber.ToString(format.NumberFormat);
            else
                FormattedString = roundedNumber.ToString(CultureInfo.InvariantCulture);
        }
        else if (cellValue.ValueType == CellValueType.Date && format.NumberFormat != null)
            FormattedString = (cellValue.GetValue<DateTime>()).ToString(format.NumberFormat);
        else
            FormattedString = Value?.ToString() ?? string.Empty;

        var cf = sheet.ConditionalFormats.GetFormatResult(row, col);
        if (cf != null)
            format.Merge(cf);

        Row = row;
        Col = col;

        Width = sheet.Columns.GetVisualWidthBetween(Col, Col + VisibleColSpan);
        Height = sheet.Rows.GetVisualHeightBetween(Row, Row + VisibleRowSpan);

        IsVisible = cell.IsVisible;

        FormatStyleString =
            GetCellFormatStyleString(Row, Col, format, cell.IsValid, cellValue.ValueType, sheet, Width, Height);
        Icon = format.Icon;
        CellType = cell.Type;
        Format = format;
    }

    private VisualCell()
    {
    }

    private static string GetCellFormatStyleString(int row, int col, CellFormat? format, bool isCellValid,
        CellValueType type, Sheet sheet, double cellWidth, double cellHeight)
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
        if (type == CellValueType.Number && format.HorizontalTextAlign == null)
        {
            sb.AddStyle("justify-content", "end");
            sb.AddStyle("text-align", "end");
        }
        else if (format.HorizontalTextAlign != null)
        {
            if (format.HorizontalTextAlign == TextAlign.Start)
            {
                sb.AddStyle("justify-content", "start");
                sb.AddStyle("text-align", "start");
            }
            else if (format.HorizontalTextAlign == TextAlign.End)
            {
                sb.AddStyle("justify-content", "end");
                sb.AddStyle("text-align", "end");
            }
            else if (format.HorizontalTextAlign == TextAlign.Center)
            {
                sb.AddStyle("justify-content", "center");
                sb.AddStyle("text-align", "center");
            }
        }

        if (format.VerticalTextAlign != null)
        {
            if (format.VerticalTextAlign == TextAlign.Start)
                sb.AddStyle("align-items", "start");
            else if (format.VerticalTextAlign == TextAlign.End)
                sb.AddStyle("align-items", "end");
            else if (format.VerticalTextAlign == TextAlign.Center)
                sb.AddStyle("align-items", "center");
        }

        if (format.TextWrap == TextWrapping.Wrap)
        {
            sb.AddStyle("text-wrap", "wrap");
        }


        return sb.ToString();
    }
}