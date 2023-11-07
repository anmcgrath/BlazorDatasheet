using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Formats;

public sealed class ConditionalFormat : ConditionalFormatAbstractBase
{
    /// <summary>
    /// The function that returns the conditional format, based on the cell's value
    /// and all other cell values in the sheet
    /// </summary>
    public Func<IReadOnlyCell, IEnumerable<IReadOnlyCell>, CellFormat>? FormatFuncDependent { get; }

    /// <summary>
    /// The function that returns the conditional format, based only on one cell's value
    /// </summary>
    public Func<IReadOnlyCell, CellFormat>? FormatFunc { get; }

    private List<IReadOnlyCell> _cellCache;

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
    public ConditionalFormat(Func<CellPosition, Sheet, bool> rule,
        Func<IReadOnlyCell, IEnumerable<IReadOnlyCell>, CellFormat> formatFuncDependent)
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
    public ConditionalFormat(Func<CellPosition, Sheet, bool> rule, Func<IReadOnlyCell, CellFormat> formatFunc) : this()
    {
        this.Predicate = rule;
        FormatFunc = formatFunc;
    }

    public override void Prepare(List<SheetRange> ranges)
    {
        _cellCache = ranges.SelectMany(x => x.GetCells()).ToList();
    }

    public override CellFormat? CalculateFormat(int row, int col, Sheet sheet)
    {
        var cell = sheet.Cells.GetCell(row, col);
        if (IsShared)
        {
            var cells = _cellCache;
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