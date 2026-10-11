using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    /// <summary>
    /// Only invalid cells have an entry (<c>false</c>). A missing entry means the cell is valid, so
    /// the store stays empty on sheets without validators instead of mirroring every written cell.
    /// </summary>
    private readonly IMatrixDataStore<bool?> _validStore = new SparseMatrixStoreByRows<bool?>();

    internal void ValidateRegion(IRegion region)
    {
        var cellsAffected = _dataStore.GetNonEmptyPositions(region).ToList();
        foreach (var (row, col) in cellsAffected)
        {
            var cellData = this.GetCellValue(row, col);
            SetValidityWithoutRestoreData(row, col, cellData);
        }

        Sheet.MarkDirty(region);
    }

    /// <summary>
    /// Validates <paramref name="value"/> at the position and records the result, returning the
    /// restore data for the validity entry.
    /// </summary>
    private MatrixRestoreData<bool?> SetValidity(int row, int col, CellValue value)
    {
        if (Sheet.Validators.IsEmpty || Sheet.Validators.Validate(value, row, col).IsValid)
            return _validStore.Clear(row, col);

        return _validStore.Set(row, col, false);
    }

    private void SetValidityWithoutRestoreData(int row, int col, CellValue value)
    {
        if (Sheet.Validators.IsEmpty || Sheet.Validators.Validate(value, row, col).IsValid)
            _validStore.ClearWithoutRestoreData(row, col);
        else
            _validStore.SetWithoutRestoreData(row, col, false);
    }

    public bool IsValid(int row, int col)
    {
        var valid = _validStore.Get(row, col);
        return valid != false;
    }
}