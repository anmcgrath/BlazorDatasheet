using BlazorDatasheet.Data;
using BlazorDatasheet.Formats;

namespace BlazorDatasheet.Render;

public class ConditionalFormat : ConditionalFormatAbstractBase
{
    /// <summary>
    /// The function that returns the conditional format, based on the cell's value
    /// and all other cell values in the sheet
    /// </summary>
    public Func<Cell, IEnumerable<Cell>, Format>? FormatFuncDependent { get; private set; }

    /// <summary>
    /// The function that returns the conditional format, based only on one cell's value
    /// </summary>
    public Func<Cell, Format>? FormatFunc { get; }

    private ConditionalFormat()
    {
    }

    /// <summary>
    /// Creates a conditional format that can be applied to cells in the sheet.
    /// The conditional format is evaluated on cell re-render and should be based on either the
    /// specific cell's value or the values of all cells that the conditional formats apply to.
    /// </summary>
    /// <param name="rule">The rule determining whether the conditional format is applied</param>
    /// <param name="formatFuncDependent">The function determining the actual format to apply, based on both the single cell's
    /// value and all cells that the conditional format applies to.</param>
    public ConditionalFormat(Func<(int row, int col), Sheet, bool> rule,
        Func<Cell, IEnumerable<Cell>, Format> formatFuncDependent)
        : this()
    {
        Predicate = rule;
        FormatFuncDependent = formatFuncDependent;
        IsShared = true;
    }

    /// <summary>
    /// Creates a conditional format that can be applied to cells in the sheet.
    /// The conditional format is evaluated on cell re-render and should be based on either the
    /// specific cell's value or the values of all cells that the conditional formats apply to.
    /// </summary>
    /// <param name="rule">The rule determining whether the conditional format is applied</param>
    /// <param name="formatFunc">The function determining the actual format to apply, based on both the single cell's
    /// value and all cells that the conditional format applies to.</param>
    public ConditionalFormat(Func<(int row, int col), Sheet, bool> rule, Func<Cell, Format> formatFunc) : this()
    {
        this.Predicate = rule;
        FormatFunc = formatFunc;
    }

    private IEnumerable<Cell> cellCache;

    public override void Prepare(Sheet sheet)
    {
        cellCache = this.GetCells(sheet);
    }

    public override Format? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cell = sheet.GetCell(row, col);
        if (IsShared)
        {
            var cells = cellCache;
            if (FormatFuncDependent != null)
                return FormatFuncDependent?.Invoke(cell, cells);
            return FormatFunc?.Invoke(cell);
        }
        else
        {
            return FormatFunc?.Invoke(cell);
        }
    }
}