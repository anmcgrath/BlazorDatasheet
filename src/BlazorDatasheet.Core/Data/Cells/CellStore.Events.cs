using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Data;
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
        if (_cellsChanged.Count > 0 || _regionsChanged.Count > 0 && _isBatchingChanges)
        {
            var args = new CellDataChangedEventArgs(_regionsChanged, _cellsChanged);
            CellsChanged?.Invoke(this, args);
        }

        _isBatchingChanges = false;
    }

    /// <summary>
    /// Raises (or records) a change for a single cell.
    /// </summary>
    private void EmitCellChanged(int row, int col)
    {
        if (_isBatchingChanges)
        {
            _cellsChanged.Add(new CellPosition(row, col));
            return;
        }

        EmitCellsChanged(new[] { new CellPosition(row, col) });
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
            return;
        }

        EmitCellsChanged([region]);
    }

    private void EmitCellsChanged(IEnumerable<IRegion> regions)
    {
        if (_isBatchingChanges)
        {
            _regionsChanged.AddRange(regions);
        }
        else
        {
            CellsChanged?.Invoke(this,
                new CellDataChangedEventArgs(regions, Enumerable.Empty<CellPosition>()));
        }
    }
}