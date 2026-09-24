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

    /// <summary>
    /// Whether the cell's value is a number.
    /// </summary>
    public bool IsNumber { get; private set; }

    /// <summary>
    /// Roundings of a General number in decreasing precision, to show when
    /// <see cref="FormattedString"/> does not fit. Explicit number formats have none.
    /// </summary>
    public string[] NumberFallbacks { get; private set; } = [];

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
    /// <param name="numberOverflow">How a number that does not fit inside the cell is displayed.</param>
    internal VisualCell(int row, int col, Sheet sheet, int numberOfSignificantDigits,
        NumberOverflowOptions numberOverflow = default)
        : this(row, col, sheet, numberOfSignificantDigits,
            AxisMetrics.ForRow(sheet, row), AxisMetrics.ForColumn(sheet, col), numberOverflow)
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
        in AxisMetrics rowMetrics, in AxisMetrics colMetrics, NumberOverflowOptions numberOverflow = default)
    {
        Row = row;
        Col = col;
        Merge = sheet.Cells.GetMerge(row, col)?.GetIntersection(sheet.Region);

        UpdateMergeSpans(sheet);

        UpdateSize(sheet, rowMetrics, colMetrics);

        // the sheet is queried directly rather than through a SheetCell facade - the facade would be
        // an extra allocation per cell and each of its properties round-trips back to the sheet anyway.
        var format = sheet.GetFormatForRendering(row, col);

        var cellValue = sheet.Cells.GetCellValue(row, col);
        Value = cellValue.Data;

        if (cellValue.ValueType == CellValueType.Number)
        {
            IsNumber = true;
            var roundedNumber = Math.Round(cellValue.GetValue<double>(), numberOfSignificantDigits);
            FormattedString = format?.NumberFormat != null
                ? roundedNumber.ToString(format.NumberFormat)
                : roundedNumber.ToString(CultureInfo.InvariantCulture);
            if (format?.NumberFormat == null && numberOverflow.Mode == NumberOverflowMode.RoundToFit)
            {
                _generalNumber = roundedNumber;
                _generalNumberMinDecimals = numberOverflow.MinDecimals;
                SetGeneralNumberFallbacks(roundedNumber, numberOverflow.MinDecimals);
            }
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

        HorizontalAlign = ResolveHorizontalAlign(format, cellValue.ValueType);
        VerticalAlign = ResolveVerticalAlign(format);
        ClassString = GetCellClassString(format, cellValue.ValueType);
        if (cellValue.ValueType == CellValueType.Number)
            ClassString += " bds-cell-number";
        if (!string.IsNullOrWhiteSpace(format?.CssClass))
            ClassString += " " + format.CssClass;
        if (format?.CornerFlagTopLeft != null || format?.CornerFlagTopRight != null ||
            format?.CornerFlagBottomLeft != null || format?.CornerFlagBottomRight != null)
            ClassString += " bds-cell-has-flags";

        FormatStyleString =
            GetCellFormatStyleString(format, sheet.Cells.IsValid(row, col));
        Icon = format?.Icon;
        CellType = sheet.Cells.GetCellType(row, col);
        Format = format;
    }

    private VisualCell()
    {
    }

    private static readonly string[] DecimalPlaceFormats =
        Enumerable.Range(0, 16).Select(d => d == 0 ? "0" : "0." + new string('#', d)).ToArray();

    private static readonly string[] ScientificDecimalPlaceFormats =
        DecimalPlaceFormats.Select(f => f + "E+00").ToArray();

    // Only used to decide whether a number has so much room that no rounding can ever be needed.
    // The font size is a css variable the host may override, so it is assumed generous: the
    // estimate must never claim a number fits when it might not.
    private const double AssumedFontSizePx = 18; // --sheet-font-size defaults to 0.75rem = 12px
    private const double AssumedCharWidthRatio = 0.7;
    private const double CellHorizontalPaddingPx = 14; // 2 x --sheet-cell-padding-horizontal + borders

    // Kept so that the chain can be regenerated when the column is resized, since a resize patches
    // the cell in place rather than rebuilding it.
    private double _generalNumber;
    private int _generalNumberMinDecimals = -1;

    /// <summary>
    /// Generates complete rounded values, in decreasing precision. The browser chooses the first
    /// that fits, so which of them is shown does not depend on a column width or an estimated font.
    /// The width is only used to skip the chain entirely for a number that has room to spare
    /// whatever the font, and the chain is worked out again if the column is resized.
    /// </summary>
    private void SetGeneralNumberFallbacks(double number, int minDecimals)
    {
        var fractionStart = FormattedString.IndexOf('.');
        if (fractionStart < 0 || !double.IsFinite(number))
            return;

        // A number with room to spare can never need a rounding, and the chain is pure cost for it.
        if (Width > 0 &&
            FormattedString.Length * AssumedCharWidthRatio * AssumedFontSizePx + CellHorizontalPaddingPx < Width)
            return;

        // Scientific values keep their exponent in every candidate.
        var exponentStart = FormattedString.IndexOf('E');
        var formats = exponentStart >= 0 ? ScientificDecimalPlaceFormats : DecimalPlaceFormats;
        var decimals = (exponentStart >= 0 ? exponentStart : FormattedString.Length) - fractionStart - 1;
        var seen = new HashSet<string> { FormattedString };
        var fallbacks = new List<string>();
        for (var d = Math.Min(decimals - 1, formats.Length - 1); d >= Math.Max(0, minDecimals); d--)
        {
            var rounded = number.ToString(formats[d], CultureInfo.InvariantCulture);
            if (seen.Add(rounded))
                fallbacks.Add(rounded);
        }

        NumberFallbacks = fallbacks.ToArray();
    }

    /// <summary>
    /// Refreshes the size, visibility and merge spans of the cell after its row or column was
    /// resized or hidden. Nothing else about a cell changes in that case - the value, format,
    /// merge region and cell type are all unaffected - so the cell is patched in place rather
    /// than rebuilt.
    /// </summary>
    internal void RefreshAxisMetrics(Sheet sheet, in AxisMetrics rowMetrics, in AxisMetrics colMetrics)
    {
        var previousWidth = Width;
        UpdateMergeSpans(sheet);
        UpdateSize(sheet, rowMetrics, colMetrics);

        // The fallback chain is only built for a number that could overflow its column, so a column
        // that has changed width needs it worked out again.
        if (_generalNumberMinDecimals >= 0 && Width != previousWidth)
        {
            NumberFallbacks = [];
            SetGeneralNumberFallbacks(_generalNumber, _generalNumberMinDecimals);
        }
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

    private static void AddPatternStyles(StyleBuilder sb, CellBackgroundPattern pattern)
    {
        var spacing = double.IsFinite(pattern.Spacing) && pattern.Spacing > 0 ? pattern.Spacing : 8;
        var stroke = double.IsFinite(pattern.StrokeSize) && pattern.StrokeSize > 0
            ? Math.Min(pattern.StrokeSize, spacing) : Math.Min(1, spacing);
        var gap = spacing.ToString(CultureInfo.InvariantCulture) + "px";
        var width = stroke.ToString(CultureInfo.InvariantCulture) + "px";
        string Lines(int angle) => $"repeating-linear-gradient({angle}deg, {pattern.Color} 0, {pattern.Color} {width}, transparent {width}, transparent {gap})";
        var image = pattern.Kind switch
        {
            CellBackgroundPatternKind.Horizontal => Lines(0),
            CellBackgroundPatternKind.Vertical => Lines(90),
            CellBackgroundPatternKind.Diagonal => Lines(45),
            CellBackgroundPatternKind.Crosshatch => Lines(45) + ", " + Lines(135),
            CellBackgroundPatternKind.Dots => $"radial-gradient(circle, {pattern.Color} {width}, transparent {width})",
            _ => null
        };
        sb.AddStyleNotNull("background-image", image);
        if (pattern.Kind == CellBackgroundPatternKind.Dots)
            sb.AddStyle("background-size", $"{gap} {gap}");
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
        if (format.BackgroundPattern is { } pattern)
            AddPatternStyles(sb, pattern);
        if (format.CssVariables is { } variables)
            foreach (var variable in variables)
                sb.AddStyle(variable.Key, variable.Value);
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
