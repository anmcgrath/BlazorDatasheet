using BlazorDatasheet.Data;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Commands;

/// <summary>
/// Clears cell values in the given ranges
/// </summary>
public class ClearCellsCommand : IUndoableCommand
{
    private readonly BRange _range;
    private List<(int row, int col, Cell cell)> _clearedCells;

    public ClearCellsCommand(BRange range)
    {
        _range = range.Clone();
    }

    public bool Execute(Sheet sheet)
    {
        var nonEmptyPositions = _range.GetNonEmptyPositions().ToList();
        _clearedCells = sheet.CellDataStore.Clear(nonEmptyPositions).ToList();
        sheet.MarkDirty(nonEmptyPositions);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Selection.Set(_range);
        sheet.SetCells(_clearedCells);
        return true;
    }
}