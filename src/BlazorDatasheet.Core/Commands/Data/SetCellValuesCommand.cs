using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValuesCommand : BaseCommand, IUndoableCommand
{
    private readonly IEnumerable<IEnumerable<CellValue>> _cellValues;
    private readonly int _row;
    private readonly int _col;
    private readonly CellStoreRestoreData _restoreData = new();

    /// <summary>
    /// Creates a command to set multiple cell values starting at position <paramref name="row"/>/<paramref name="col"/>
    /// </summary>
    /// <param name="values">The values to set. values[row] is the row offset from <paramref name="row"/>. Each values[row][col] is the col offset from <paramref name="col"/></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public SetCellValuesCommand(int row, int col, CellValue[][] values)
    {
        _cellValues = values;
        _row = row;
        _col = col;
    }


    /// <summary>
    /// Creates a command to set multiple cell values starting at position <paramref name="row"/>/<paramref name="col"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="values"></param>
    public SetCellValuesCommand(int row, int col, IEnumerable<IEnumerable<CellValue>> values)
    {
        _cellValues = values;
        _row = row;
        _col = col;
    }

    /// <summary>
    /// Sets all the cells in the region <paramref name="region"/> to <paramref name="value"/>
    /// </summary>
    /// <param name="region"></param>
    /// <param name="value"></param>
    public SetCellValuesCommand(IRegion region, CellValue value)
    {
        _row = region.Top;
        _col = region.Left;
        _cellValues = Enumerable.Repeat(Enumerable.Repeat(value, region.Width), region.Height);
    }

    public override bool CanExecute(Sheet sheet) => true;

    public override bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.BatchUpdates();
        ExecuteSetCellValueData(sheet);
        sheet.EndBatchUpdates();
        sheet.ScreenUpdating = true;

        return true;
    }

    private void ExecuteSetCellValueData(Sheet sheet)
    {
        var rowEnd = _row;
        var colEnd = _col;

        int rowOffset = 0;

        foreach (var row in _cellValues)
        {
            int colOffset = 0;
            
            foreach (var value in row)
            {
                _restoreData.Merge(sheet.Cells.SetValueImpl(_row + rowOffset, _col + colOffset, value));
                colEnd = Math.Max(colEnd, _col + colOffset);
                colOffset++;
            }

            rowEnd = Math.Max(rowEnd, _row + rowOffset);
            rowOffset++;
        }

        sheet.MarkDirty(new Region(_row, rowEnd, _col, colEnd));
    }


    public bool Undo(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.BatchUpdates();
        sheet.Cells.Restore(_restoreData);
        sheet.EndBatchUpdates();
        sheet.ScreenUpdating = true;
        return true;
    }
}