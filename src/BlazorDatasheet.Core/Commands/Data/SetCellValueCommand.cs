using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands.Data;

public class SetCellValueCommand : BaseCommand, IUndoableCommand
{
    public readonly int Row;
    public readonly int Col;
    public readonly CellValue Value;
    private CellStoreRestoreData _restoreData = null!;

    /// <summary>
    /// Sets a single cell value to the <paramref name="value"/>. No conversion is performed.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    public SetCellValueCommand(int row, int col, CellValue value)
    {
        Row = row;
        Col = col;
        Value = value;
    }

    public override bool CanExecute(Sheet sheet)
    {
        return sheet.Region.Contains(Row, Col);
    }

    public override bool Execute(Sheet sheet)
    {
        sheet.ScreenUpdating = false;
        _restoreData = sheet.Cells.SetValueImpl(Row, Col, Value);

        sheet.MarkDirty(new RowRegion(Row));
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