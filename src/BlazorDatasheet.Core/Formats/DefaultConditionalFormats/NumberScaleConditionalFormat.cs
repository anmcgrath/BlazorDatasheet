using System.Drawing;
using System.Text.Json.Serialization;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using ColorConverter = BlazorDatasheet.Core.Color.ColorConverter;

namespace BlazorDatasheet.Core.Formats.DefaultConditionalFormats;

public class NumberScaleConditionalFormat : ConditionalFormatAbstractBase
{
    public System.Drawing.Color ColorStart { get; }
    public System.Drawing.Color? ColorMid { get; }
    public System.Drawing.Color ColorEnd { get; }
    public System.Drawing.Color[]? ColorStops { get; }
    public int LutSize { get; }

    public bool AutoTextColor { get; set; }

    private string[] _computedLut = Array.Empty<string>();
    private string[] _computedTextLut = Array.Empty<string>();
    private double cachedMin;
    private double cachedMax;
    private double cachedMid;

    [JsonConstructor]
    public NumberScaleConditionalFormat(System.Drawing.Color colorStart, System.Drawing.Color colorEnd,
        System.Drawing.Color? colorMid = null, System.Drawing.Color[]? colorStops = null, int lutSize = 20)
    {
        ColorStart = colorStart;
        ColorMid = colorMid;
        ColorEnd = colorEnd;
        ColorStops = colorStops is { Length: >= 2 } ? colorStops.ToArray() : null;
        LutSize = Math.Max(lutSize, 2);
        ComputeLUT(LutSize);
        this.IsShared = true;
    }

    private NumberScaleConditionalFormat(System.Drawing.Color[] stops, int lutSize)
    {
        ColorStops = stops.ToArray();
        LutSize = Math.Max(lutSize, 2);
        ColorStart = stops[0];
        ColorEnd = stops[^1];
        ComputeLUT(LutSize);
        this.IsShared = true;
    }

    private void ComputeLUT(int size)
    {
        _computedLut = new string[size];
        _computedTextLut = new string[size];

        System.Drawing.Color[] stops;
        if (ColorStops != null)
            stops = ColorStops;
        else if (ColorMid == null)
            stops = new[] { ColorStart, ColorEnd };
        else
            stops = new[] { ColorStart, ColorMid.Value, ColorEnd };

        var labStops = stops.Select(ColorConverter.RgbToOklab).ToArray();

        for (int i = 0; i < size; i++)
        {
            var frac = i / (double)(size - 1);
            var scaled = frac * (labStops.Length - 1);
            var idx = Math.Min((int)scaled, labStops.Length - 2);
            var t = scaled - idx;
            var labInterp = ColorConverter.OklabInterp(labStops[idx], labStops[idx + 1], t);
            var newColor = ColorConverter.OklabToRgb(labInterp.l, labInterp.a, labInterp.b);
            _computedLut[i] = $"rgb({newColor.R},{newColor.G},{newColor.B})";
            _computedTextLut[i] = labInterp.l < 0.65 ? "rgb(255,255,255)" : "rgb(0,0,0)";
        }
    }

    public override void Prepare(List<SheetRange> ranges)
    {
        var cells = ranges.SelectMany(x => x.GetNonEmptyCells());
        double sum = 0;
        int count = 0;
        var max = double.MinValue;
        var min = double.MaxValue;

        foreach (var cell in cells)
        {
            if (cell.ValueType != CellValueType.Number)
                continue;

            var val = cell.GetValue<double?>();
            if (val == null)
                continue;
            sum += val.Value;
            count++;
            if (val > max)
                max = val.Value;
            if (val < min)
                min = val.Value;
        }

        cachedMax = max;
        cachedMin = min;
        cachedMid = sum / count;
    }

    private int GetLutIndex(double value)
    {
        var size = Math.Abs(cachedMax - cachedMin);
        if (size == 0)
            return 0;

        double frac;
        if (ColorMid == null)
        {
            frac = (value - cachedMin) / size;
        }
        else
        {
            // If 3-color scale, we map [min, mid] to [0, 0.5] and [mid, max] to [0.5, 1]
            if (value <= cachedMid)
            {
                var range = cachedMid - cachedMin;
                if (range == 0) frac = 0.5;
                else frac = 0.5 * (value - cachedMin) / range;
            }
            else
            {
                var range = cachedMax - cachedMid;
                if (range == 0) frac = 1;
                else frac = 0.5 + 0.5 * (value - cachedMid) / range;
            }
        }

        var index = (int)(frac * (_computedLut.Length - 1));
        return Math.Clamp(index, 0, _computedLut.Length - 1);
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cell = sheet.Cells.GetCell(row, col);
        if (cell.ValueType != CellValueType.Number)
            return null;

        var value = cell.GetValue<double?>();
        if (value == null)
            return null;

        var index = GetLutIndex(value.Value);
        var format = new CellFormat()
        {
            BackgroundColor = _computedLut[index]
        };

        if (AutoTextColor)
            format.ForegroundColor = _computedTextLut[index];

        return format;
    }

    #region Palettes

    public static NumberScaleConditionalFormat RedYellowGreen =>
        new(ColorTranslator.FromHtml("#F8696B"), ColorTranslator.FromHtml("#63BE7B"),
            ColorTranslator.FromHtml("#FFEB84"));

    public static NumberScaleConditionalFormat BlueWhiteRed =>
        new(ColorTranslator.FromHtml("#5A8AC6"), ColorTranslator.FromHtml("#F8696B"),
            ColorTranslator.FromHtml("#FCFCFF"));

    public static NumberScaleConditionalFormat RedWhiteBlue =>
        new(ColorTranslator.FromHtml("#F8696B"), ColorTranslator.FromHtml("#5A8AC6"),
            ColorTranslator.FromHtml("#FCFCFF"));

    public static NumberScaleConditionalFormat Viridis =>
        new(new[]
        {
            ColorTranslator.FromHtml("#440154"),
            ColorTranslator.FromHtml("#482878"),
            ColorTranslator.FromHtml("#3E4A89"),
            ColorTranslator.FromHtml("#31688E"),
            ColorTranslator.FromHtml("#26828E"),
            ColorTranslator.FromHtml("#1F9E89"),
            ColorTranslator.FromHtml("#35B779"),
            ColorTranslator.FromHtml("#6DCE59"),
            ColorTranslator.FromHtml("#B4DE2C"),
            ColorTranslator.FromHtml("#FDE725"),
        }, 40);

    public static NumberScaleConditionalFormat Sunset =>
        new(ColorTranslator.FromHtml("#F3E79B"), ColorTranslator.FromHtml("#5D3161"));

    #endregion
}
