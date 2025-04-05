using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValueCommand : BaseCommand, IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly CellValue _value;
    private CellStoreRestoreData _restoreData = null!;

    /// <summary>
    /// Sets a single cell value to the <paramref name="value"/>. No conversion is performed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    public SetCellValueCommand(int row, int col, CellValue value)
    {
        _row = row;
        _col = col;
        _value = value;
    }

    public override bool CanExecute(Sheet sheet)
    {
        return sheet.Region.Contains(_row, _col);
    }

    public override bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        _restoreData = sheet.Cells.SetValueImpl(_row, _col, _value);

        sheet.MarkDirty(new RowRegion(_row));
        sheet.ScreenUpdating = true;
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        sheet.Cells.Restore(_restoreData);
        sheet.ScreenUpdating = true;
        return true;
    }
}