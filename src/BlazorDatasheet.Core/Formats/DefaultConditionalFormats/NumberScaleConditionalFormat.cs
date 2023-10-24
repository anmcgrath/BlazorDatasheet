using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Formats.DefaultConditionalFormats;

public class NumberScaleConditionalFormat : ConditionalFormatAbstractBase
{
    public NumberScaleConditionalFormat()
    {
        this.IsShared = true;
    }

    private double cachedMean;
    private double cachedMin;
    private double cachedMax;

    public override void Prepare(Sheet sheet)
    {
        var cells = this.GetCells(sheet);
        double sum = 0;
        int count = 0;
        var max = double.MinValue;
        var min = double.MaxValue;

        foreach (var cell in cells)
        {
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

        base.Prepare(sheet);
    }

    private string getColour(double value, double min, double max, double mean)
    {
        byte R;
        byte G;
        byte B;
        var size = Math.Abs(max - min);
        var frac = (value - min)/size;
        R = (byte)(255 * frac);
        G = (byte)(255 * frac);
        B = (byte)(255 * frac);
        var str = $"rgb({R},{G},{B})";
        return str;
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cell = sheet.GetCell(row, col);
        var value = cell.GetValue<double?>();
        if (value == null)
            return null;

        return new CellFormat()
        {
            BackgroundColor = getColour(value.Value, cachedMin, cachedMax, cachedMean)
        };
    }
}