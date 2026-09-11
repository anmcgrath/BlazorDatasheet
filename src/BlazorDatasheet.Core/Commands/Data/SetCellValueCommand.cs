using BlazorDatasheet.Core.Protection;
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

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.CanEdit(Row, Col);

    protected override bool CanExecuteCore(Sheet sheet)
    {
        return sheet.ContainsPosition(Row, Col);
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        using var updates = sheet.SuspendUpdates();
        _restoreData = sheet.Cells.SetValueImpl(Row, Col, Value);
        sheet.MarkDirty(Row, Col);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        using var updates = sheet.SuspendUpdates();
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}