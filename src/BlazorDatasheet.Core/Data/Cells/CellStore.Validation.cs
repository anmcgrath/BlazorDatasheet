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
    private readonly IMatrixDataStore<bool> _validStore = new SparseMatrixStore<bool>();
    
    internal void ValidateRegion(IRegion region)
    {
        var cellsAffected = _dataStore.GetNonEmptyPositions(region).ToList();
        foreach (var (row, col) in cellsAffected)
        {
            var cellData = _dataStore.Get(row, col);
            var result = _sheet.Validators.Validate(cellData, row, col);
            _validStore.Set(row, col, result.IsValid);
        }

        _sheet.MarkDirty(cellsAffected);
    }

}