using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Selection;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Selecting;

public class Selection
{
    private readonly Sheet _sheet;

    /// <summary>
    /// The region that is active for accepting user input, usually the most recent region added
    /// </summary>
    public IRegion? ActiveRegion { get; private set; }

    private readonly List<IRegion> _regions = new();

    /// <summary>
    /// All regions in the selection.
    /// </summary>
    public IReadOnlyList<IRegion> Regions => _regions;

    public IEnumerable<SheetRange> Ranges => _regions.Select(x => _sheet.Range(x));

    /// <summary>
    /// The position of the cell that is "active" in the selection.
    /// It is sometimes but not always the same as the input position.
    /// </summary>
    public CellPosition ActiveCellPosition { get; private set; }

    /// <summary>
    /// The region that is currently being selected
    /// </summary>
    public IRegion? SelectingRegion { get; private set; }

    /// <summary>
    /// The current mode of selecting
    /// </summary>
    private SelectionMode _selectingMode;

    /// <summary>
    /// The position that the selecting process was started at
    /// </summary>
    public CellPosition SelectingStartPosition { get; private set; }

    public bool IsSelecting => SelectingRegion != null;

    /// <summary>
    /// Fired when the current selection changes
    /// </summary>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    /// <summary>
    /// Fired when the current selecting region changes
    /// </summary>
    public event EventHandler<IRegion?>? SelectingChanged;

    /// <summary>
    /// Fired before the active cell position changes. Allows the modification of the new active cell position.
    /// </summary>
    public event EventHandler<BeforeActiveCellPositionChangedEventArgs>? BeforeActiveCellPositionChanged;

    /// <summary>
    /// Fired when the cells that are selected changes.
    /// </summary>
    public event EventHandler<CellsSelectedEventArgs>? CellsSelected;

    /// <summary>
    /// Fired when the active cell position changes.
    /// </summary>
    public EventHandler<ActiveCellPositionChangedEventArgs>? ActiveCellPositionChanged;

    public Selection(Sheet sheet)
    {
        _sheet = sheet;
    }

    #region SELECTING

    public void BeginSelectingCell(int row, int col)
    {
        if (_sheet.Area == 0)
            return;

        this.SelectingRegion = new Region(row, col);
        this.SelectingStartPosition = new CellPosition(row, col);
        this._selectingMode = SelectionMode.Cell;
        this.SelectingRegion = _sheet.ExpandRegionOverMerges(SelectingRegion);
        EmitSelectingChanged();
    }

    public void BeginSelectingCol(int col)
    {
        if (_sheet.Area == 0)
            return;
        this.SelectingRegion = new ColumnRegion(col, col);
        this.SelectingStartPosition = new CellPosition(0, col);
        this._selectingMode = SelectionMode.Column;
        this.SelectingRegion = _sheet.ExpandRegionOverMerges(SelectingRegion);
        EmitSelectingChanged();
    }

    public void BeginSelectingRow(int row)
    {
        if (_sheet.Area == 0)
            return;
        this.SelectingRegion = new RowRegion(row, row);
        this.SelectingStartPosition = new CellPosition(row, 0);
        this._selectingMode = SelectionMode.Row;
        this.SelectingRegion = _sheet.ExpandRegionOverMerges(SelectingRegion);
        EmitSelectingChanged();
    }

    public void UpdateSelectingEndPosition(int row, int col)
    {
        if (SelectingRegion == null)
            return;

        switch (_selectingMode)
        {
            case SelectionMode.Column:
                SelectingRegion = new ColumnRegion(SelectingStartPosition.col, col);
                break;
            case SelectionMode.Row:
                SelectingRegion = new RowRegion(SelectingStartPosition.row, row);
                break;
            case SelectionMode.Cell:
                SelectingRegion = new Region(SelectingStartPosition.row, row, SelectingStartPosition.col, col);
                break;
        }

        SelectingRegion = _sheet.ExpandRegionOverMerges(SelectingRegion);
        EmitSelectingChanged();
    }

    public void CancelSelecting()
    {
        SelectingRegion = null;
        EmitSelectingChanged();
    }

    public void EndSelecting()
    {
        if (SelectingRegion == null)
            return;
        SetActiveCellPosition(SelectingStartPosition.row, SelectingStartPosition.col);
        var oldRegions = Regions.Select(x => x.Clone()).ToList();
        this.Add(SelectingRegion);
        SelectingRegion = null;
        EmitSelectingChanged();
        EmitSelectionChange(oldRegions);
    }

    private void EmitSelectingChanged()
    {
        SelectingChanged?.Invoke(this, SelectingRegion);
    }

    #endregion

    #region SELECTION

    /// <summary>
    /// Clears any selection regions
    /// </summary>
    public void ClearSelections()
    {
        var oldRegions = CloneRegions();
        _regions.Clear();
        ActiveRegion = null;
        EmitSelectionChange(oldRegions);
    }

    /// <summary>
    /// Adds the region to the selection
    /// </summary>
    /// <param name="region"></param>
    private void Add(IRegion region) => Add(new List<IRegion>() { region });

    /// <summary>
    /// Adds the region to the selection
    /// </summary>
    /// <param name="regions"></param>
    private void Add(List<IRegion> regions)
    {
        var oldRegions = CloneRegions();

        if (_sheet.Area == 0 || !regions.Any())
            return;

        foreach (var region in regions)
        {
            var expandedRegion = _sheet.ExpandRegionOverMerges(region);
            if (expandedRegion != null)
                _regions.Add(expandedRegion);
        }

        ActiveRegion = _regions.LastOrDefault();
        EmitSelectionChange(oldRegions);
    }

    /// <summary>
    /// Extends the active position to the row/col specified
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void ExtendTo(int row, int col)
    {
        var oldRegions = CloneRegions();

        if (ActiveRegion == null)
            return;

        var newRegion = new Region(ActiveCellPosition.row, row, ActiveCellPosition.col, col);
        var expanded = _sheet.ExpandRegionOverMerges(newRegion);

        if (expanded != null)
            ActiveRegion.Set(expanded);
        EmitSelectionChange(oldRegions);
    }

    /// <summary>
    /// Expands the edge of the active region by the amount specified, or the next visible.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="amount"></param>
    public void ExpandEdge(Edge edge, int amount)
    {
        var oldRegions = CloneRegions();

        if (ActiveRegion == null)
            return;

        var dir = edge == Edge.Top || edge == Edge.Left ? -1 : 1;
        var edgePosition = ActiveRegion.GetEdgePosition(edge);
        var axis = GetAxis(edge);

        var newRegion = ActiveRegion.Clone();

        var nextVisible = _sheet.GetRowColStore(axis).GetNextVisible(edgePosition, dir);
        newRegion.Expand(edge, Math.Max(Math.Abs(amount), Math.Abs(nextVisible - edgePosition)));

        var expanded = _sheet.ExpandRegionOverMerges(newRegion);
        expanded = expanded?.GetIntersection(_sheet.Region);
        if (expanded != null)
            ActiveRegion.Set(expanded);

        EmitSelectionChange(oldRegions);
    }

    private Axis GetAxis(Edge edge)
    {
        if (edge == Edge.Bottom || edge == Edge.Top)
            return Axis.Row;
        else
            return Axis.Col;
    }

    /// <summary>
    /// Contracts the edge of the active region by the amount specified, or the next visible.
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="amount"></param>
    public void ContractEdge(Edge edge, int amount)
    {
        var oldRegions = CloneRegions();

        if (ActiveRegion == null)
            return;

        var dir = edge == Edge.Top || edge == Edge.Left ? 1 : -1;
        var edgePosition = ActiveRegion.GetEdgePosition(edge);
        var axis = GetAxis(edge);

        var nextVisible = _sheet.GetRowColStore(axis).GetNextVisible(edgePosition, dir);

        var newRegion = ActiveRegion.Clone();
        newRegion.Contract(edge, Math.Abs(nextVisible - edgePosition));

        var contracted = _sheet.ContractRegionOverMerges(newRegion);
        var activeCellRegion = _sheet.Cells.GetMerge(ActiveCellPosition.row, ActiveCellPosition.col) ??
                               new Region(ActiveCellPosition.row, ActiveCellPosition.col);

        if (contracted != null &&
            (contracted.Contains(activeCellRegion) || contracted is RowRegion or ColumnRegion))
            ActiveRegion.Set(contracted);
        else
        {
            ExpandEdge(edge.GetOpposite(), amount);
        }

        EmitSelectionChange(oldRegions);
    }

    /// <summary>
    /// Clears any selections or active selections and sets the selection to the region specified
    /// </summary>
    /// <param name="region"></param>
    public void Set(IRegion region) => Set([region]);

    /// <summary>
    /// Sets selection to a single cell and clears any current selections
    /// </summary>
    /// <param name="row"></param> 
    /// <param name="col"></param>
    public void Set(int row, int col)
    {
        Set(new Region(row, col));
    }

    public void Set(List<IRegion> regions)
    {
        _regions.Clear();
        if (_sheet.Area == 0 || regions.Count == 0)
            return;

        SetActiveCellPosition(regions.First().Top, regions.First().Left);
        this.Add(regions);
    }

    public void ConstrainSelectionToSheet()
    {
        if (_sheet.Area == 0)
        {
            this.ClearSelections();
            return;
        }

        var constrainedRegions = new List<IRegion>();
        foreach (var region in _regions)
        {
            var intersection = region.GetIntersection(_sheet.Region);
            if (intersection != null)
                constrainedRegions.Add(intersection);
        }

        ActiveRegion = null;
        _regions.Clear();
        Set(constrainedRegions);
    }

    /// <summary>
    /// Returns true if the position is inside any of the active selections
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col)
    {
        if (!_regions.Any())
            return false;
        return _regions
            .Any(x => x.Contains(row, col));
    }

    /// <summary>
    /// Returns whether there are no regions in the selection
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        return !_regions.Any();
    }

    public void MoveActivePosition(Axis axis, int amount)
    {
        if (axis == Axis.Col)
            MoveActivePositionByCol(amount);
        else if (axis == Axis.Row)
            MoveActivePositionByRow(amount);
    }

    /// <summary>
    /// Move the active cell position to the next most relevant position
    /// If there's nowhere to go, collapse and move down. Otherwise, move through all regions,
    /// setting them as active where appropriate.
    /// </summary>
    /// <param name="rowDir"></param>
    public void MoveActivePositionByRow(int rowDir)
    {
        var oldRegions = CloneRegions();
        if (ActiveRegion == null || rowDir == 0)
            return;

        rowDir = Math.Sign(rowDir);

        var currRow = ActiveCellPosition.row;
        var currCol = ActiveCellPosition.col;

        // If it's currently inside a merged cell, find where it should next move to
        var merge = _sheet.Cells.GetMerge(currRow, currCol);
        if (merge != null)
        {
            // move the active row position to the edge of the current merged cell
            if (rowDir == -1)
                currRow = merge.Top;
            else
                currRow = merge.Bottom;
        }

        currRow = _sheet.Rows.GetNextVisible(currRow, rowDir);
        if (currRow == -1)
            currRow = rowDir == 1 ? ActiveRegion.Bottom + 1 : ActiveRegion.Top - 1;

        // Fix the active region to surrounds of the sheet
        var activeRegionFixed = ActiveRegion.GetIntersection(_sheet.Region);
        if (activeRegionFixed == null)
            return;

        // If the active region is only one cell and there are no other regions,
        // clear the regions and move the whole thing down
        if (_regions.Count == 1 && (activeRegionFixed.Area == 1 || activeRegionFixed.Equals(merge)))
        {
            // We end up with a new region of area 1 (or the size of the merged cell it is not on)
            _regions.Clear();

            var offsetPosn = new CellPosition(currRow, currCol);
            offsetPosn = _sheet.Region.GetConstrained(offsetPosn);
            var newRegion = new Region(offsetPosn.row, offsetPosn.col);
            newRegion = _sheet.ExpandRegionOverMerges(newRegion) as Region;
            SetActiveCellPosition(offsetPosn.row, offsetPosn.col);
            if (newRegion != null)
                this.Add(newRegion);
            EmitSelectionChange(oldRegions);
            return;
        }

        // Move the posn and attempt to bring into either the next region
        // or the next cell in the region
        var newRow = currRow;
        var newCol = currCol;
        if (newRow > activeRegionFixed.Bottom)
        {
            newCol++;
            newRow = activeRegionFixed.Top;
            if (newCol > activeRegionFixed.Right)
            {
                var newActiveRegion = GetRegionAfterActive();
                var newActiveRegionFixed = newActiveRegion.GetIntersection(_sheet.Region);
                if (newActiveRegionFixed == null)
                    return;

                newCol = newActiveRegionFixed.Left;
                newRow = newActiveRegionFixed.Top;
                ActiveRegion = newActiveRegion;
            }
        }
        else if (newRow < activeRegionFixed.Top)
        {
            newCol--;
            newRow = activeRegionFixed.Bottom;
            if (newCol < activeRegionFixed.Left)
            {
                var newActiveRegion = GetRegionAfterActive();
                var newActiveRegionFixed = newActiveRegion.GetIntersection(_sheet.Region);
                if (newActiveRegionFixed == null)
                    return;
                newCol = newActiveRegionFixed.Right;
                newRow = newActiveRegionFixed.Bottom;
                ActiveRegion = newActiveRegion;
            }
        }

        SetActiveCellPosition(newRow, newCol);
        EmitSelectionChange(oldRegions);
    }

    /// <summary>
    /// Move the active cell position to the next most relevant position
    /// If there's nowhere to go, collapse and move down. Otherwise, move through all regions,
    /// setting them as active where appropriate.
    /// </summary>
    /// <param name="colDir"></param>
    public void MoveActivePositionByCol(int colDir)
    {
        var oldRegions = CloneRegions();
        if (ActiveRegion == null || colDir == 0)
            return;

        colDir = Math.Sign(colDir);

        var currRow = ActiveCellPosition.row;
        var currCol = ActiveCellPosition.col;

        // If it's currently inside a merged cell, find where it should next move to
        var merge = _sheet.Cells.GetMerge(ActiveCellPosition.row, ActiveCellPosition.col);
        if (merge != null)
        {
            // move the active row position to the edge of the current merged cell
            if (colDir == -1)
                currCol = merge.Left;
            else
                currCol = merge.Right;
        }

        currCol = _sheet.Columns.GetNextVisible(currCol, colDir);
        if (currCol == -1)
            currCol = colDir == 1 ? ActiveRegion.Right + 1 : ActiveRegion.Left - 1;

        // Fix the active region to surrounds of the sheet
        var activeRegionFixed = ActiveRegion.GetIntersection(_sheet.Region);
        if (activeRegionFixed == null)
            return;

        // If the active region is only one cell and there are no other regions,
        // clear the regions and move the whole thing down
        if (_regions.Count == 1 && (activeRegionFixed.Area == 1 || activeRegionFixed.Equals(merge)))
        {
            // We end up with a new region of area 1 (or the size of the merged cell it is not on)
            _regions.Clear();

            var offsetPosn = new CellPosition(currRow, currCol);
            offsetPosn = _sheet.Region.GetConstrained(offsetPosn);
            var newRegion = new Region(offsetPosn.row, offsetPosn.col);
            newRegion = _sheet.ExpandRegionOverMerges(newRegion) as Region;
            SetActiveCellPosition(offsetPosn.row, offsetPosn.col);
            if (newRegion != null)
                this.Add(newRegion);

            EmitSelectionChange(oldRegions);
            return;
        }

        // Move the posn and attempt to bring into either the next region
        // or the next cell in the region
        var newRow = ActiveCellPosition.row;
        var newCol = ActiveCellPosition.col + colDir;
        if (newCol > activeRegionFixed.Right)
        {
            newCol = activeRegionFixed.Left;
            newRow++;
            if (newRow > activeRegionFixed.Bottom)
            {
                var newActiveRegion = GetRegionAfterActive();
                var newActiveRegionFixed = newActiveRegion.GetIntersection(_sheet.Region);
                if (newActiveRegionFixed == null)
                    return;
                newCol = newActiveRegionFixed.Left;
                newRow = newActiveRegionFixed.Top;
                ActiveRegion = newActiveRegion;
            }
        }
        else if (newCol < activeRegionFixed.Left)
        {
            newCol = activeRegionFixed.Right;
            newRow--;
            if (newRow < activeRegionFixed.Top)
            {
                var newActiveRegion = GetRegionAfterActive();
                var newActiveRegionFixed = newActiveRegion.GetIntersection(_sheet.Region);
                if (newActiveRegionFixed == null)
                    return;
                newCol = newActiveRegionFixed.Right;
                newRow = newActiveRegionFixed.Bottom;
                ActiveRegion = newActiveRegion;
            }
        }

        SetActiveCellPosition(newRow, newCol);
        EmitSelectionChange(oldRegions);
    }

    private IRegion GetRegionAfterActive()
    {
        var activeRegionIndex = _regions.IndexOf(ActiveRegion!);
        if (activeRegionIndex == -1)
            throw new Exception("No range is active?");
        activeRegionIndex++;
        if (activeRegionIndex >= _regions.Count)
            activeRegionIndex = 0;
        return _regions[activeRegionIndex];
    }

    private IRegion GetRegionBeforeActive()
    {
        var activeRegionIndex = _regions.IndexOf(ActiveRegion!);
        if (activeRegionIndex == -1)
            throw new Exception("No range is active?");
        activeRegionIndex--;
        if (activeRegionIndex < 0)
            activeRegionIndex = _regions.Count - 1;
        return _regions[activeRegionIndex];
    }

    private void EmitSelectionChange(IReadOnlyList<IRegion> oldRegions)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldRegions, _regions, _sheet));
        CellsSelected?.Invoke(this, new CellsSelectedEventArgs(_sheet.Cells.GetCellsInRegions(_regions)));
    }

    /// <summary>
    /// Returns the position that should receive input
    /// </summary>
    /// <returns></returns>
    public CellPosition GetInputPosition()
    {
        if (this.ActiveRegion == null)
            throw new Exception("Invalid cell position");

        // Check whether there are any merged regions at the ActiveCellPosition.
        // If there are, the input position is the top left of the merged,
        // Otherwise it corresponds to the active cell position.
        var merged = _sheet.Cells.GetMerge(ActiveCellPosition.row, ActiveCellPosition.col);
        if (merged == null)
            return ActiveCellPosition;
        else
            return new CellPosition(merged.TopLeft.row, merged.TopLeft.col);
    }

    #endregion

    public object Value
    {
        set
        {
            _sheet.Commands.BeginCommandGroup();
            foreach (var region in _regions)
                _sheet.Cells.SetValues(region, value);
            _sheet.Commands.EndCommandGroup();
        }
    }

    public void Clear()
    {
        _sheet.Cells.ClearCells(this.Regions);
    }

    internal void SetActiveCellPosition(int row, int col)
    {
        var oldPosition = ActiveCellPosition;
        var newPosition = new CellPosition(row, col);
        var beforeEventArgs = new BeforeActiveCellPositionChangedEventArgs(oldPosition, newPosition);
        BeforeActiveCellPositionChanged?.Invoke(this, beforeEventArgs);
        newPosition = beforeEventArgs.NewCellPosition;
        var setEventArgs = new ActiveCellPositionChangedEventArgs(oldPosition, newPosition);
        this.ActiveCellPosition = newPosition;
        ActiveCellPositionChanged?.Invoke(this, setEventArgs);
    }

    internal SelectionSnapshot GetSelectionSnapshot()
    {
        var activeRegionIndex = ActiveRegion != null ? _regions.IndexOf(ActiveRegion!) : -1;
        var regions = _regions.Select(x => x.Clone()).ToList();
        var activeRegionClone = activeRegionIndex != -1 ? regions[activeRegionIndex] : null;
        return new SelectionSnapshot(activeRegionClone, regions, ActiveCellPosition);
    }

    internal void Restore(SelectionSnapshot selectionSnapshot)
    {
        var oldRegions = CloneRegions();
        this.ActiveRegion = selectionSnapshot.ActiveRegion;
        _regions.Clear();
        _regions.AddRange(selectionSnapshot.Regions);
        ActiveCellPosition = selectionSnapshot.ActiveCellPosition;
        EmitSelectionChange(oldRegions);
    }

    private IReadOnlyList<IRegion> CloneRegions()
    {
        return _regions.Select(x => x.Clone()).ToList();
    }
}