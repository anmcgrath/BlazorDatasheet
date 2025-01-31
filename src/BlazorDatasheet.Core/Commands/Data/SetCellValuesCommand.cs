using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValuesCommand : CommandGroup, IUndoableCommand
{
    /// <summary>
    /// Creates a command to set multiple cell values starting at position <paramref name="row"/>/<paramref name="col"/>
    /// </summary>
    /// <param name="values">The values to set. values[row] is the row offset from <paramref name="row"/>. Each values[row][col] is the col offset from <paramref name="col"/></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public SetCellValuesCommand(int row, int col, object[][] values)
    {
        for (int rIndex = 0; rIndex < values.Length; rIndex++)
        {
            int rowActual = row + rIndex;
            for (int cIndex = 0; cIndex < values[rIndex].Length; cIndex++)
            {
                int colActual = col + cIndex;
                this.AddCommand(new SetCellValueCommand(rowActual, colActual, values[rIndex][cIndex]));
            }
        }
    }

    /// <summary>
    /// Creates a command to set multiple cell values starting at position <paramref name="row"/>/<paramref name="col"/>
    /// </summary>
    /// <param name="values">The values to set. values[row] is the row offset from <paramref name="row"/>. Each values[row][col] is the col offset from <paramref name="col"/></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public SetCellValuesCommand(int row, int col, CellValue[][] values)
    {
        for (int rIndex = 0; rIndex < values.Length; rIndex++)
        {
            int rowActual = row + rIndex;
            for (int cIndex = 0; cIndex < values[rIndex].Length; cIndex++)
            {
                int colActual = col + cIndex;
                this.AddCommand(new SetCellValueCommand(rowActual, colActual, values[rIndex][cIndex]));
            }
        }
    }

    /// <summary>
    /// Sets all the cells in the region <paramref name="region"/> to <paramref name="value"/>
    /// </summary>
    /// <param name="region"></param>
    /// <param name="value"></param>
    public SetCellValuesCommand(IRegion region, object? value) : this(region, new CellValue(value))
    {
    }

    /// <summary>
    /// Sets all the cells in the region <paramref name="region"/> to <paramref name="value"/>
    /// </summary>
    /// <param name="region"></param>
    /// <param name="value"></param>
    public SetCellValuesCommand(IRegion region, CellValue value)
    {
        for (int r = region.Top; r <= region.Bottom; r++)
        {
            for (int c = region.Left; c <= region.Right; c++)
            {
                this.AddCommand(new SetCellValueCommand(r, c, value));
            }
        }
    }
}