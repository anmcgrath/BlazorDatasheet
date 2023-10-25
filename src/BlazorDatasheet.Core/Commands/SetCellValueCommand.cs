using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Commands;

public class SetCellValueCommand : IUndoableCommand
{
    private readonly int _row;
    private readonly int _col;
    private readonly object? _value;
    private object? _oldValue;
    private CellFormula? _oldFormula;

    public SetCellValueCommand(int row, int col, object? value)
    {
        _row = row;
        _col = col;
        _value = value;
    }

    public bool Execute(Sheet sheet)
    {
        _oldValue = sheet.Cells.CellDataStore.Get(_row, _col);
        _oldFormula = sheet.Cells.CellFormulaStore.Get(_row, _col);

        // If cell values are being set while the formula engine is not calculating,
        // then these values must override the formula and so the formula should be cleared
        // at those cell positions.
        if (!sheet.FormulaEngine.IsCalculating)
        {
            if (_oldFormula != null)
            {
                sheet.FormulaEngine.RemoveFromDependencyGraph(_row, _col);
            }

            sheet.Cells.CellFormulaStore.Clear(_row, _col);
        }

        sheet.Cells.CellDataStore.Set(_row, _col, _value);

        // Perform data validation
        // but we don't restrict the cell value being set here,
        // it is just marked as invalid if it is invalid
        var validationResult = sheet.Cells.Validation.Validate(_value, _row, _col);
        sheet.Cells.ValidationStore.Set(_row, _col, validationResult.IsValid);

        sheet.MarkDirty(_row, _col);

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        // restore formula first
        if (_oldFormula != null)
        {
            sheet.Cells.CellFormulaStore.Set(_row, _col, _oldFormula);
            sheet.FormulaEngine.AddToDependencyGraph(_row, _col, _oldFormula);
        }

        sheet.Cells.CellDataStore.Set(_row, _col, _oldValue);
        sheet.MarkDirty(_row, _col);

        return true;
    }
}