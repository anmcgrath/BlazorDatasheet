using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    /// <summary>
    /// Stores whether cells are valid.
    /// </summary>
    private readonly IMatrixDataStore<bool?> _validStore = new SparseMatrixStoreByRows<bool?>();

    internal void ValidateRegion(IRegion region)
    {
        var cellsAffected = _dataStore.GetNonEmptyPositions(region).ToList();
        foreach (var (row, col) in cellsAffected)
        {
            var cellData = this.GetCellValue(row, col);
            var result = _sheet.Validators.Validate(cellData, row, col);
            _validStore.Set(row, col, result.IsValid);
        }

        _sheet.MarkDirty(cellsAffected);
    }

    public bool IsValid(int row, int col)
    {
        var valid = _validStore.Get(row, col);
        return valid != false;
    }
}