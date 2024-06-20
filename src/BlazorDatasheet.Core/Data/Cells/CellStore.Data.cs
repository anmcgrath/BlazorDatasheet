using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    /// <summary>
    /// The cell DATA
    /// </summary>
    private readonly IMatrixDataStore<CellValue> _dataStore;

    /// <summary>
    /// Sets the cell value using <see cref="SetCellValueCommand"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetValue(int row, int col, object value)
    {
        var cmd = new SetCellValueCommand(row, col, new CellValue(value));
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the cell value using <see cref="SetCellValueCommand"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetValue(int row, int col, CellValue value)
    {
        var cmd = new SetCellValueCommand(row, col, value);
        return _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets a cell value to the value specified and raises the appropriate events.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns>Restore data that stores the changes made.</returns>
    internal CellStoreRestoreData SetValueImpl(int row, int col, CellValue value)
    {
        var restoreData = new CellStoreRestoreData();

        // If cell values are being set while the formula engine is not calculating,
        // then these values must override the formula and so the formula should be cleared
        // at those cell positions.
        if (!_sheet.FormulaEngine.IsCalculating && HasFormula(row, col))
        {
            restoreData.FormulaRestoreData = ClearFormulaImpl(row, col).FormulaRestoreData;
        }

        // Validate but don't stop setting cell values if the value is invalid.
        var validationResult = _sheet.Validators.Validate(value, row, col);

        // Save old validation result and current cell values.
        restoreData.ValidRestoreData = _validStore.Set(row, col, validationResult.IsValid);

        var newCellValue = value.IsEmpty ? _defaultCellValue : value;
        restoreData.ValueRestoreData = _dataStore.Set(row, col, newCellValue);

        this.EmitCellChanged(row, col);
        _sheet.MarkDirty(row, col);

        return restoreData;
    }

    /// <summary>
    /// Sets a cell value to the value specified and raises the appropriate events.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns>Restore data that stores the changes made.</returns>
    internal CellStoreRestoreData SetValueImpl(int row, int col, object? value)
    {
        return SetValueImpl(row, col, new CellValue(value));
    }

    /// <summary>
    /// Sets cell values to those specified using <see cref="SetCellValueCommand"/>
    /// </summary>
    /// <param name="changes"></param>
    /// <returns></returns>
    public bool SetValues(IEnumerable<(int row, int col, object value)> changes)
    {
        _sheet.Commands.BeginCommandGroup();
        foreach (var change in changes)
        {
            _sheet.Commands.ExecuteCommand(new SetCellValueCommand(change.row, change.col,
                new CellValue(change.value)));
        }

        _sheet.Commands.EndCommandGroup();

        return true;
    }

    /// <summary>
    /// Gets the cell's value at row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public object? GetValue(int row, int col)
    {
        return _dataStore.Get(row, col)!.Data;
    }

    /// <summary>
    /// Returns the raw <see cref="CellValue"/> stored at the <paramref name="row"/> and <paramref name="col"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public CellValue GetCellValue(int row, int col)
    {
        return _dataStore.Get(row, col)!;
    }

    /// <summary>
    /// Returns the sparse matrix store that holds the cell data.
    /// </summary>
    /// <returns></returns>
    internal IMatrixDataStore<CellValue> GetCellDataStore()
    {
        return _dataStore;
    }
}