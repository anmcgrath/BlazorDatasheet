using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private List<ValueChange> _removedValues;

    public RemoveColumnCommand(int columnIndex)
    {
        _columnIndex = columnIndex;
    }

    public bool Execute(Sheet sheet)
    {
        // Keep track of the values we have removed
        var nonEmptyInCol = sheet.GetNonEmptyCellPositions(new ColumnRegion(_columnIndex));
        _removedValues = nonEmptyInCol.Select(x => new ValueChange(x.row, x.col, sheet.GetValue(x.row, x.col)))
                                      .ToList();
        return sheet.RemoveColImpl(_columnIndex);
    }

    public bool Undo(Sheet sheet)
    {
        // Insert column back in and set all the values that we removed
        sheet.InsertColAfter(_columnIndex - 1);
        sheet.SetCellValuesImpl(_removedValues);
        return true;
    }
}