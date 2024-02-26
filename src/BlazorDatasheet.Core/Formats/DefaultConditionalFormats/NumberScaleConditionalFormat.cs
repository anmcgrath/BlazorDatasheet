using System.Drawing;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;
using ColorConverter = BlazorDatasheet.Core.Color.ColorConverter;

namespace BlazorDatasheet.Core.Formats.DefaultConditionalFormats;

public class NumberScaleConditionalFormat : ConditionalFormatAbstractBase
{
    private readonly System.Drawing.Color _colorStart;
    private readonly System.Drawing.Color _colorEnd;
    private string[] _computedLut;
    private double cachedMean;
    private double cachedMin;
    private double cachedMax;

    public NumberScaleConditionalFormat(System.Drawing.Color colorStart, System.Drawing.Color colorEnd)
    {
        _colorStart = colorStart;
        _colorEnd = colorEnd;
        ComputeLUT(20);
        this.IsShared = true;
    }

    private void ComputeLUT(int size)
    {
        _computedLut = new string[size];
        var hsvStart = ColorConverter.RGBToHSV(_colorStart);
        var hsvEnd = ColorConverter.RGBToHSV(_colorEnd);
        for (int i = 0; i < size; i++)
        {
            (double h, double s, double v) = ColorConverter.HsvInterp(hsvStart, hsvEnd, (i / (double)size));
            var newColor = ColorConverter.HSVToRGB(h, s, v);
            _computedLut[i] = $"rgb({newColor.R},{newColor.G},{newColor.B})";
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
        cachedMean = sum / count;
        cachedMin = min;
    }

    private string GetColourString(double value, double min, double max, double mean)
    {
        var size = Math.Abs(max - min);
        if (size == 0)
            return _computedLut.First();

        var frac = (value - min) / size;
        var index = (int)(frac * _computedLut.Length);
        var color = _computedLut[Math.Min(index, _computedLut.Length - 1)];
        return color;
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cell = sheet.Cells.GetCell(row, col);
        if (cell.ValueType != CellValueType.Number)
            return null;

        var value = cell.GetValue<double?>();
        if (value == null)
            return null;

        return new CellFormat()
        {
            BackgroundColor = GetColourString(value.Value, cachedMin, cachedMax, cachedMean)
        };
    }
}