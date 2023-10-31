using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

public class SheetRangeCell : SheetRange
{
    public new object? Value
    {
        set => base.Value = value;
        get => base.GetCells().First().GetValue();
    }

    /// <summary>
    /// A range of a single cell
    /// </summary>
    /// <param name="sheet">The sheet that the range applies to</param>
    /// <param name="row">The cell's row</param>
    /// <param name="col">The cell's column</param>
    public SheetRangeCell(Sheet sheet, int row, int col) : base(sheet, new Region(row, col))
    {
    }
}