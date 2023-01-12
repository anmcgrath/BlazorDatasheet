using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class RemoveColumnCommand : IUndoableCommand
{
    private int _columnIndex;
    private Heading _removedHeading;
    private List<ValueChange> _removedValues;
    private ClearCellsCommand _clearCellsCommand;

    public RemoveColumnCommand(int columnIndex)
    {
        _columnIndex = columnIndex;
    }

    public bool Execute(Sheet sheet)
    {
        // 1. Clear the cells using the clear cells command
        // So that we can easily undo
        _clearCellsCommand = new ClearCellsCommand(new BRange(sheet, new ColumnRegion(_columnIndex)));
        _clearCellsCommand.Execute(sheet);
        
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
        _clearCellsCommand.Undo(sheet);
        return true;
    }
}