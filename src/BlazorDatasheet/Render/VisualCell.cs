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
    public string ClassString { get; private set; } = "bds-sheet-cell";
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
        : this(row, col, sheet, numberOfSignificantDigits,
            AxisMetrics.ForRow(sheet, row), AxisMetrics.ForColumn(sheet, col))
    {
    }

    /// <summary>
    /// Creates a visual cell using row and column metrics that have already been resolved.
    /// </summary>
    /// <remarks>
    /// A whole viewport is built one row at a time, and the row height, column width and
    /// visibility of each are the same for every cell along that row or column. Resolving them
    /// once per row/column instead of once per cell removes several cumulative-size binary
    /// searches per cell.
    /// </remarks>
    internal VisualCell(int row, int col, Sheet sheet, int numberOfSignificantDigits,
        in AxisMetrics rowMetrics, in AxisMetrics colMetrics)
    {
        Row = row;
        Col = col;
        Merge = sheet.Cells.GetMerge(row, col)?.GetIntersection(sheet.Region);

        UpdateMergeSpans(sheet);

        // the sheet is queried directly rather than through a SheetCell facade - the facade would be
        // an extra allocation per cell and each of its properties round-trips back to the sheet anyway.
        var format = sheet.GetFormatForRendering(row, col);

        var cellValue = sheet.Cells.GetCellValue(row, col);
        Value = cellValue.Data;

        if (cellValue.ValueType == CellValueType.Number)
        {
            var roundedNumber = Math.Round(cellValue.GetValue<double>(), numberOfSignificantDigits);
            if (format?.NumberFormat != null)
                FormattedString = roundedNumber.ToString(format.NumberFormat);
            else
                FormattedString = roundedNumber.ToString(CultureInfo.InvariantCulture);
        }
        else if (cellValue.ValueType == CellValueType.Date && format?.NumberFormat != null)
            FormattedString = (cellValue.GetValue<DateTime>()).ToString(format.NumberFormat);
        else
            FormattedString = Value?.ToString() ?? string.Empty;

        var cf = sheet.ConditionalFormats.GetFormatResult(row, col);
        if (cf != null)
        {
            format ??= new CellFormat();
            format.Merge(cf);
        }

        UpdateSize(sheet, rowMetrics, colMetrics);

        HorizontalAlign = ResolveHorizontalAlign(format, cellValue.ValueType);
        VerticalAlign = ResolveVerticalAlign(format);
        ClassString = GetCellClassString(format, cellValue.ValueType);

        FormatStyleString =
            GetCellFormatStyleString(format, sheet.Cells.IsValid(row, col));
        Icon = format?.Icon;
        CellType = sheet.Cells.GetCellType(row, col);
        Format = format;
    }

    private VisualCell()
    {
    }

    /// <summary>
    /// Refreshes the size, visibility and merge spans of the cell after its row or column was
    /// resized or hidden. Nothing else about a cell changes in that case - the value, format,
    /// merge region and cell type are all unaffected - so the cell is patched in place rather
    /// than rebuilt.
    /// </summary>
    internal void RefreshAxisMetrics(Sheet sheet, in AxisMetrics rowMetrics, in AxisMetrics colMetrics)
    {
        UpdateMergeSpans(sheet);
        UpdateSize(sheet, rowMetrics, colMetrics);
    }

    private void UpdateMergeSpans(Sheet sheet)
    {
        if (Merge == null)
            return;

        VisibleMergeColStart = sheet.Columns.GetNextVisible(Merge.Left - 1);
        VisibleMergeRowStart = sheet.Rows.GetNextVisible(Merge.Top - 1);

        IsMergeStart = Row == VisibleMergeRowStart && Col == VisibleMergeColStart;

        VisibleRowSpan = sheet.Rows.CountVisible(VisibleMergeRowStart, Merge.Bottom);
        VisibleColSpan = sheet.Columns.CountVisible(VisibleMergeColStart, Merge.Right);
    }

    private void UpdateSize(Sheet sheet, in AxisMetrics rowMetrics, in AxisMetrics colMetrics)
    {
        Width = Merge == null
            ? colMetrics.Size
            : sheet.Columns.GetVisualWidthBetween(Merge.Left, Merge.Right + 1);
        Height = Merge == null
            ? rowMetrics.Size
            : sheet.Rows.GetVisualHeightBetween(Merge.Top, Merge.Bottom + 1);

        IsVisible = rowMetrics.IsVisible && colMetrics.IsVisible;
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

    /// <summary>
    /// Returns shared class strings for alignment instead of creating identical inline style
    /// strings for each cell. Numeric cells hit the end-aligned fast path by default.
    /// </summary>
    private static string GetCellClassString(IReadonlyCellFormat? format, CellValueType type)
    {
        var horizontal = format?.HorizontalTextAlign != null || type == CellValueType.Number
            ? ResolveHorizontalAlign(format, type)
            : (TextAlign?)null;
        var vertical = format?.VerticalTextAlign;

        return (horizontal, vertical) switch
        {
            (null, null) => "bds-sheet-cell",
            (TextAlign.Start, null) => "bds-sheet-cell bds-cell-align-start",
            (TextAlign.Center, null) => "bds-sheet-cell bds-cell-align-center",
            (TextAlign.End, null) => "bds-sheet-cell bds-cell-align-end",
            (null, TextAlign.Start) => "bds-sheet-cell bds-cell-valign-start",
            (null, TextAlign.Center) => "bds-sheet-cell bds-cell-valign-center",
            (null, TextAlign.End) => "bds-sheet-cell bds-cell-valign-end",
            (TextAlign.Start, TextAlign.Start) => "bds-sheet-cell bds-cell-align-start bds-cell-valign-start",
            (TextAlign.Start, TextAlign.Center) => "bds-sheet-cell bds-cell-align-start bds-cell-valign-center",
            (TextAlign.Start, TextAlign.End) => "bds-sheet-cell bds-cell-align-start bds-cell-valign-end",
            (TextAlign.Center, TextAlign.Start) => "bds-sheet-cell bds-cell-align-center bds-cell-valign-start",
            (TextAlign.Center, TextAlign.Center) => "bds-sheet-cell bds-cell-align-center bds-cell-valign-center",
            (TextAlign.Center, TextAlign.End) => "bds-sheet-cell bds-cell-align-center bds-cell-valign-end",
            (TextAlign.End, TextAlign.Start) => "bds-sheet-cell bds-cell-align-end bds-cell-valign-start",
            (TextAlign.End, TextAlign.Center) => "bds-sheet-cell bds-cell-align-end bds-cell-valign-center",
            (TextAlign.End, TextAlign.End) => "bds-sheet-cell bds-cell-align-end bds-cell-valign-end",
            _ => "bds-sheet-cell"
        };
    }

    private static string GetCellFormatStyleString(CellFormat? format, bool isCellValid)
    {
        // an unformatted, valid cell contributes no inline style at all - which is most cells on
        // most sheets. Default numeric alignment is supplied by ClassString.
        if (isCellValid && (format == null || format.IsDefaultFormat()))
            return string.Empty;

        var sb = new StyleBuilder();

        if (!isCellValid)
            sb.AddStyle("color", "var(--invalid-cell-foreground-color)");
        else if (format != null)
            sb.AddStyle("color", format.ForegroundColor!, format.ForegroundColor != null);

        if (format == null)
            return sb.ToString();

        sb.AddStyle("background-color", format.BackgroundColor!, format.BackgroundColor != null);
        sb.AddStyle("font-weight", format.FontWeight!, format.FontWeight != null);
        sb.AddStyle("font-style", format.FontStyle!, format.FontStyle != null);
        sb.AddStyle("text-decoration", format.TextDecoration!, format.TextDecoration != null);

        if (format.BorderBottom != null)
            sb.AddStyle("border-bottom", $"{format.BorderBottom.Width}px solid {format.BorderBottom.Color};");
        if (format.BorderRight != null)
            sb.AddStyle("border-right", $"{format.BorderRight.Width}px solid {format.BorderRight.Color};");

        if (format.TextWrap == TextWrapping.Wrap)
        {
            sb.AddStyle("text-wrap", "wrap");
        }


        return sb.ToString();
    }
}