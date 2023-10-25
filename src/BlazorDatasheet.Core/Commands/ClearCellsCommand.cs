using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Clears cell values in the given ranges
/// </summary>
public class ClearCellsCommand : IUndoableCommand
{
    private readonly BRange _range;
    private List<(int row, int col, object? data)> _clearedData;
    private List<(int row, int col, CellFormula? formula)> _clearedFormula;

    public ClearCellsCommand(BRange range)
    {
        _range = range.Clone();
    }

    public bool Execute(Sheet sheet)
    {
        _clearedData = sheet.CellDataStore.Clear(_range.Regions).ToList();
        _clearedFormula = sheet.CellFormulaStore.Clear(_range.Regions).ToList();
        foreach (var formula in _clearedFormula)
        {
            sheet.FormulaEngine.RemoveFromDependencyGraph(formula.row, formula.col);
        }

        var clearedPositions = _clearedData.Select(x => (x.row, x.col))
            .Concat(_clearedFormula.Select((x => (x.row, x.col))));
        
        sheet.EmitCellsChanged(clearedPositions);
        sheet.MarkDirty(_clearedData.Select(x => (x.row, x.col)));
        sheet.MarkDirty(_clearedFormula.Select(x => (x.row, x.col)));
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Selection.Set(_range);
        sheet.CellDataStore.Restore(_clearedData);
        sheet.CellFormulaStore.Restore(_clearedFormula);

        foreach (var formula in _clearedFormula)
        {
            sheet.FormulaEngine.AddToDependencyGraph(formula.row, formula.col, formula.formula);
        }

        sheet.MarkDirty(_clearedData.Select(x => (x.row, x.col)));
        sheet.MarkDirty(_clearedFormula.Select(x => (x.row, x.col)));

        return true;
    }
}