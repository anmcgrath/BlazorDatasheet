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
            var result = Validation.Validate(cellData, row, col);
            _validStore.Set(row, col, result.IsValid);
        }

        _sheet.MarkDirty(cellsAffected);
    }

    /// <summary>
    /// Add a <see cref="IDataValidator"> to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validator"></param>
    public void AddValidator(IRegion region, IDataValidator validator)
    {
        var cmd = new SetValidatorCommand(region, validator);
        _sheet.Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Adds multiple validators to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validators"></param>
    public void AddValidators(IRegion region, IEnumerable<IDataValidator> validators)
    {
        _sheet.Commands.BeginCommandGroup();
        foreach (var validator in validators)
        {
            AddValidator(region, validator);
        }

        _sheet.Commands.EndCommandGroup();
    }

}