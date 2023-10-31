using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data.Cells;

public partial class CellStore
{
    public event EventHandler<CellMetaDataChangeEventArgs>? MetaDataChanged;
    public event EventHandler<CellFormulaChangeEventArgs>? FormulaChanged;
    
    /// <summary>
    /// If batching changes, they are stored here.
    /// </summary>
    private readonly HashSet<CellPosition> _cellsChanged = new();

    /// <summary>
    /// Fired when one or more cells are changed
    /// </summary>
    public event EventHandler<IEnumerable<CellPosition>>? CellsChanged;

    private bool _isBatchingChanges = false;

    internal void BatchChanges()
    {
        if (!_isBatchingChanges)
        {
            _cellsChanged.Clear();
        }

        _isBatchingChanges = true;
    }

    internal void EndBatchChanges()
    {
        if (_cellsChanged.Any() && _isBatchingChanges)
            CellsChanged?.Invoke(this, _cellsChanged.AsEnumerable());

        _isBatchingChanges = false;
    }

    private void EmitCellChanged(int row, int col)
    {
        this.EmitCellsChanged(new[] { new CellPosition(row, col) });
    }

    private void EmitCellsChanged(IEnumerable<CellPosition> positions)
    {
        if (_isBatchingChanges)
        {
            foreach (var pos in positions)
                _cellsChanged.Add(pos);
        }
        else
        {
            CellsChanged?.Invoke(this, positions);
        }
    }
}