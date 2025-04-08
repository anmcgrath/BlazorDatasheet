using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.DataStructures.Geometry;
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
    /// Fired before a cell value conversion (from object to <see cref="CellValue"/>) occurs. The value can be modified.
    /// </summary>
    public event EventHandler<BeforeCellValueConversionEventArgs> BeforeCellValueConversion;

    /// <summary>
    /// Sets the cell value using <see cref="SetCellValueCommand"/>
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public bool SetValue(int row, int col, object? value)
    {
        var cmd = new SetCellValueCommand(row, col, ConvertToCellValue(row, col, value));
        return Sheet.Commands.ExecuteCommand(cmd);
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
        return Sheet.Commands.ExecuteCommand(cmd);
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
        if (!Sheet.FormulaEngine.IsCalculating && HasFormula(row, col))
        {
            restoreData.FormulaRestoreData = ClearFormulaImpl(row, col).FormulaRestoreData;
        }

        // Validate but don't stop setting cell values if the value is invalid.
        var validationResult = Sheet.Validators.Validate(value, row, col);

        // Save old validation result and current cell values.
        restoreData.ValidRestoreData = _validStore.Set(row, col, validationResult.IsValid);

        if (value.IsEmpty)
            restoreData.ValueRestoreData = _dataStore.Clear(row, col);
        else
            restoreData.ValueRestoreData = _dataStore.Set(row, col, value);

        this.EmitCellChanged(row, col);

        return restoreData;
    }

    /// <summary>
    /// Sets a cell value to the value specified and raises the appropriate events. <paramref name="value"/> is implicitly converted to a cell value.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns>Restore data that stores the changes made.</returns>
    internal CellStoreRestoreData SetValueImpl(int row, int col, object? value)
    {
        return SetValueImpl(row, col, ConvertToCellValue(row, col, value));
    }

    /// <summary>
    /// Sets values, starting from <paramref name="row"/>,<paramref name="col"/>. Cell values are implicitly converted.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="values">An array of rows, with each row having a number of values.</param>
    /// <returns></returns>
    public bool SetValues(int row, int col, object[][] values)
    {
        var cmd = new SetCellValuesCommand(row, col,
            values.Select((r, rowOffset) =>
                r.Select((v, colOffset) => ConvertToCellValue(row + rowOffset, col + colOffset, v))));
        return Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets values, starting from <paramref name="row"/>,<paramref name="col"/>. Cell values are not converted in this case.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="values">An array of rows, with each row having a number of values.</param>
    /// <returns></returns>
    public bool SetValues(int row, int col, CellValue[][] values)
    {
        var cmd = new SetCellValuesCommand(row, col, values);
        return Sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets all values in the region to the single value supplied.
    /// </summary>
    /// <returns></returns>
    public bool SetValues(IRegion region, object? value)
    {
        if (region.IsSingleCell())
            return SetValue(region.Top, region.Left, value);

        var boundRegion = region.GetIntersection(Sheet.Region);
        if (boundRegion == null)
            return true;

        var cellValues = new CellValue[boundRegion.Height][];
        for (int i = 0; i < boundRegion.Height; i++)
        {
            cellValues[i] = new CellValue[boundRegion.Width];
            for (int j = 0; j < boundRegion.Width; j++)
            {
                cellValues[i][j] = ConvertToCellValue(i + boundRegion.Top, j + boundRegion.Left, value);
            }
        }

        return SetValues(boundRegion.Top, boundRegion.Left, cellValues);
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


    /// <summary>
    /// Converts an object value to the cell value. If the cell has a type set, it will use the type to guide the conversion.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    internal CellValue ConvertToCellValue(int row, int col, object? value)
    {
        CellValue newValue;
        var type = GetCellType(row, col);
        if (value == null)
            newValue = CellValue.Empty;
        else
        {
            newValue = type switch
            {
                "text" => CellValue.Text(value.ToString() ?? string.Empty),
                _ => new CellValue(value)
            };
        }

        var args = new BeforeCellValueConversionEventArgs(value, newValue, row, col, type);
        BeforeCellValueConversion?.Invoke(this, args);
        return args.NewValue;
    }

    /// <summary>
    /// Returns the next visible cell with data in the row specified, and after (given <paramref name="colDir"/>) the column index <paramref name="colIndex"/>
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="colIndex"></param>
    /// <param name="colDir"></param>
    /// <returns></returns>
    public SheetCell? GetNextInRow(int rowIndex, int colIndex, int colDir = 1)
    {
        var nextIndex = GetNextRowIndexInRow(rowIndex, colIndex, colDir);
        if (nextIndex == -1)
            return null;
        return new SheetCell(rowIndex, nextIndex, Sheet);
    }

    /// <summary>
    /// Returns the next visible cell with data in the row specified, and after (given <paramref name="colDir"/>) the column index <paramref name="colIndex"/>
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="colIndex"></param>
    /// <param name="colDir"></param>
    /// <returns></returns>
    public int GetNextRowIndexInRow(int rowIndex, int colIndex, int colDir = 1)
    {
        var nextColIndex = _dataStore.GetNextNonEmptyIndexInRow(rowIndex, colIndex, colDir);
        if (nextColIndex == -1)
            return -1;

        if (!Sheet.Columns.IsVisible(nextColIndex))
            return GetNextRowIndexInRow(rowIndex, Sheet.Columns.GetNextVisible(nextColIndex, colDir));

        if (nextColIndex >= Sheet.NumCols || nextColIndex < 0)
            return -1;

        return nextColIndex;
    }
}