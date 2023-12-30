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
    /// If batching data changes, the changed regions are stored here.
    /// </summary>
    private readonly List<IRegion> _regionsChanged = new();

    /// <summary>
    /// Fired when one or more cells are changed
    /// </summary>
    public event EventHandler<CellDataChangedEventArgs>? CellsChanged;

    private bool _isBatchingChanges = false;

    internal void BatchChanges()
    {
        if (!_isBatchingChanges)
        {
            _cellsChanged.Clear();
            _regionsChanged.Clear();
        }

        _isBatchingChanges = true;
    }

    internal void EndBatchChanges()
    {
        if (_cellsChanged.Any() || _regionsChanged.Any() && _isBatchingChanges)
        {
            var args = new CellDataChangedEventArgs(_regionsChanged, _cellsChanged);
            CellsChanged?.Invoke(this, args);
        }

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
            CellsChanged?.Invoke(this, new CellDataChangedEventArgs(Enumerable.Empty<IRegion>(), positions));
        }
    }

    private void EmitCellsChanged(IRegion region)
    {
        if (_isBatchingChanges)
        {
            _regionsChanged.Add(region);
        }
        else
        {
            CellsChanged?.Invoke(this,
                new CellDataChangedEventArgs(new[] { region }, Enumerable.Empty<CellPosition>()));
        }
    }
}