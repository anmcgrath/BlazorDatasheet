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
    /// The horizontal alignment the cell is actually rendered with, after applying defaults.
    /// </summary>
    public TextAlign HorizontalAlign { get; private set; } = TextAlign.Start;

    /// <summary>
    /// The vertical alignment the cell is actually rendered with, after applying defaults.
    /// </summary>
    public TextAlign VerticalAlign { get; private set; } = TextAlign.Start;


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

        Width = Merge == null
            ? sheet.Columns.GetVisualWidthBetween(Col, Col + 1)
            : sheet.Columns.GetVisualWidthBetween(Merge.Left, Merge.Right + 1);
        Height = Merge == null
            ? sheet.Rows.GetVisualHeightBetween(Row, Row + 1)
            : sheet.Rows.GetVisualHeightBetween(Merge.Top, Merge.Bottom + 1);

        IsVisible = cell.IsVisible;
        HorizontalAlign = ResolveHorizontalAlign(format, cellValue.ValueType);
        VerticalAlign = ResolveVerticalAlign(format);

        FormatStyleString =
            GetCellFormatStyleString(Row, Col, format, cell.IsValid, cellValue.ValueType, sheet, Width, Height);
        Icon = format.Icon;
        CellType = cell.Type;
        Format = format;
    }

    private VisualCell()
    {
    }

    /// <summary>
    /// Resolves the horizontal alignment a cell is rendered with. Numbers sit at the end of the
    /// cell unless the format says otherwise.
    /// </summary>
    internal static TextAlign ResolveHorizontalAlign(IReadonlyCellFormat? format, CellValueType type)
    {
        if (format?.HorizontalTextAlign != null)
            return format.HorizontalTextAlign.Value;

        return type == CellValueType.Number ? TextAlign.End : TextAlign.Start;
    }

    /// <summary>
    /// Resolves the vertical alignment a cell is rendered with.
    /// </summary>
    internal static TextAlign ResolveVerticalAlign(IReadonlyCellFormat? format)
        => format?.VerticalTextAlign ?? TextAlign.Start;

    /// <summary>
    /// The physical CSS keyword for horizontal text alignment.
    /// </summary>
    internal static string ToCssTextAlign(TextAlign align) => align switch
    {
        TextAlign.Center => "center",
        TextAlign.End => "right",
        _ => "left"
    };

    /// <summary>
    /// The CSS keyword for flex alignment.
    /// </summary>
    internal static string ToCssFlexAlign(TextAlign align) => align switch
    {
        TextAlign.Center => "center",
        TextAlign.End => "end",
        _ => "start"
    };

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
        sb.AddStyle("font-style", format.FontStyle!, format.FontStyle != null);
        sb.AddStyle("text-decoration", format.TextDecoration!, format.TextDecoration != null);

        if (format.BorderBottom != null)
            sb.AddStyle("border-bottom", $"{format.BorderBottom.Width}px solid {format.BorderBottom.Color};");
        if (format.BorderRight != null)
            sb.AddStyle("border-right", $"{format.BorderRight.Width}px solid {format.BorderRight.Color};");

        // numbers move to the right when no align is set, otherwise the defaults already apply
        if (format.HorizontalTextAlign != null || type == CellValueType.Number)
        {
            var horizontalAlign = ResolveHorizontalAlign(format, type);
            sb.AddStyle("justify-content", ToCssFlexAlign(horizontalAlign));
            sb.AddStyle("text-align", ToCssTextAlign(horizontalAlign));
        }

        if (format.VerticalTextAlign != null)
            sb.AddStyle("align-items", ToCssFlexAlign(ResolveVerticalAlign(format)));

        if (format.TextWrap == TextWrapping.Wrap)
        {
            sb.AddStyle("text-wrap", "wrap");
        }


        return sb.ToString();
    }
}
