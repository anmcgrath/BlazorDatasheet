using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class RemoveRowCommand : IUndoableCommand
{
    private readonly int _rowIndex;
    private List<CellChange> _valuesRemoved;

    /// <summary>
    /// Command to remove the row at the index given.
    /// </summary>
    /// <param name="rowIndex">The row to remove.</param>
    public RemoveRowCommand(int rowIndex)
    {
        _rowIndex = rowIndex;
        _valuesRemoved = new List<CellChange>();
    }

    public bool Execute(Sheet sheet)
    {
        // Keep track of values that we have removed
        var nonEmptyPosns = sheet.GetNonEmptyCellPositions(new RowRegion(_rowIndex));
        _valuesRemoved = nonEmptyPosns.Select(x => new CellChange(x.row, x.col, sheet.GetValue(x.row, x.col)))
                                      .ToList();
        return sheet.RemoveRowAtImpl(_rowIndex);
    }

    public bool Undo(Sheet sheet)
    {
        sheet.InsertRowAfterImpl(_rowIndex - 1);
        sheet.SetCellValuesImpl(_valuesRemoved);
        return true;
    }
}