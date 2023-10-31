using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Clears cell values in the given ranges
/// </summary>
public class ClearCellsCommand : IUndoableCommand
{
    private readonly SheetRange _range;
    private CellStoreRestoreData _restoreData;

    public ClearCellsCommand(SheetRange range)
    {
        _range = range.Clone();
    }

    public bool Execute(Sheet sheet)
    {
        _restoreData = sheet.Cells.ClearCellsImpl(_range.Regions);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Selection.Set(_range);
        sheet.Cells.Restore(_restoreData);
        return true;
    }
}