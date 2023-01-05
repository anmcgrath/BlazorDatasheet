using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private Heading _removedHeading;
    private List<ValueChange> _removedValues;

    public RemoveColumnCommand(int columnIndex)
    {
        _columnIndex = columnIndex;
    }

    public bool Execute(Sheet sheet)
    {
        // Keep track of the values we have removed
        var nonEmptyInCol = sheet.GetNonEmptyCellPositions(new ColumnRegion(_columnIndex));
        _removedValues = nonEmptyInCol.Select(x => new ValueChange(x.row, x.col, sheet.GetCellValue(x.row, x.col)))
                                      .ToList();
        if (sheet.ColumnHeadings.Any() && _columnIndex >= 0 && _columnIndex < sheet.ColumnHeadings.Count)
            _removedHeading = sheet.ColumnHeadings[_columnIndex];

        var res = sheet.RemoveColImpl(_columnIndex);
        return res;
    }

    public bool Undo(Sheet sheet)
    {
        // Insert column back in and set all the values that we removed
        sheet.InsertColAfterImpl(_columnIndex - 1);
        if (_columnIndex >= 0 && _columnIndex < sheet.ColumnHeadings.Count)
            sheet.ColumnHeadings[_columnIndex] = _removedHeading;
        sheet.SetCellValuesImpl(_removedValues);
        return true;
    }
}