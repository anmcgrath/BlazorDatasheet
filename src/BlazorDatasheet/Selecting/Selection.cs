using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Selecting;

public class Selection : BRange
{
    private Sheet _sheet;

    /// <summary>
    /// The region that is active for accepting user input, usually the most recent region added
    /// </summary>
    public IRegion? ActiveRegion { get; private set; }

    /// <summary>
    /// The region that is currently being selected
    /// </summary>
    internal IRegion? SelectingRegion { get; private set; }

    /// <summary>
    /// The current mode of selecting
    /// </summary>
    private SelectionMode SelectingMode { get; set; }

    /// <summary>
    /// The position that the selecting process was started at
    /// </summary>
    internal CellPosition SelectingStartPosition { get; private set; }

    internal bool IsSelecting => SelectingRegion != null;

    /// <summary>
    /// The position of the cell that is "active" in the selection.
    /// It is sometimes but not always the same as the input position.
    /// </summary>
    public CellPosition ActiveCellPosition { get; private set; }

    /// <summary>
    /// Fired when the current selection changes
    /// </summary>
    public event EventHandler<IEnumerable<IRegion>> SelectionChanged;

    /// <summary>
    /// Fired when the current selecting region changes
    /// </summary>
    public event EventHandler<IRegion> SelectingChanged;

    public Selection(Sheet sheet) : base(sheet, new List<IRegion>())
    {
        _sheet = sheet;
    }

    #region SELECTING

    public void BeginSelectingCell(int row, int col)
    {
        this.SelectingRegion = new Region(row, col);
        this.SelectingStartPosition = new CellPosition(row, col);
        this.SelectingMode = SelectionMode.Cell;
        this.SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
        emitSelectingChanged();
    }

    public void BeginSelectingCol(int col)
    {
        this.SelectingRegion = new ColumnRegion(col, col);
        this.SelectingStartPosition = new CellPosition(0, col);
        this.SelectingMode = SelectionMode.Column;
        this.SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
        emitSelectingChanged();
    }

    public void BeginSelectingRow(int row)
    {
        this.SelectingRegion = new RowRegion(row, row);
        this.SelectingStartPosition = new CellPosition(row, 0);
        this.SelectingMode = SelectionMode.Row;
        this.SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
        emitSelectingChanged();
    }

    public void UpdateSelectingEndPosition(int row, int col)
    {
        if (SelectingRegion == null)
            return;

        switch (SelectingMode)
        {
            case SelectionMode.Column:
                SelectingRegion = new ColumnRegion(SelectingStartPosition.Col, col);
                break;
            case SelectionMode.Row:
                SelectingRegion = new RowRegion(SelectingStartPosition.Row, row);
                break;
            case SelectionMode.Cell:
                SelectingRegion = new Region(SelectingStartPosition.Row, row, SelectingStartPosition.Col, col);
                break;
        }

        SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
        emitSelectingChanged();
    }

    public void CancelSelecting()
    {
        SelectingRegion = null;
        emitSelectingChanged();
    }

    public void EndSelecting()
    {
        if (SelectingRegion == null)
            return;
        this.ActiveCellPosition = new CellPosition(SelectingStartPosition.Row, SelectingStartPosition.Col);
        this.AddRegionToSelections(SelectingRegion);
        SelectingStartPosition = new CellPosition(-1, -1);
        SelectingRegion = null;
        emitSelectingChanged();
    }

    private void emitSelectingChanged()
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
        _regions.Clear();
        ActiveRegion = null;
        emitSelectionChange();
    }

    /// <summary>
    /// Adds the region to the selection
    /// </summary>
    /// <param name="region"></param>
    internal void AddRegionToSelections(IRegion region)
    {
        _regions.Add(region);
        ActiveRegion = ExpandRegionOverMerged(region);
        emitSelectionChange();
    }

    /// <summary>
    /// Expands the active selection so that it covers any merged cells
    /// </summary>
    private IRegion? ExpandRegionOverMerged(IRegion? region)
    {
        // Look at the four sides of the active region
        // If any of the sides are touching active regions, we check whether the selection
        // covers the region entirely. If not, expand the sides so that they cover.
        // Continue until there are no more merge intersections that we don't fully cover.
        var boundedRegion = region.Copy();

        if (boundedRegion == null)
            return null;

        List<CellMerge> mergeOverlaps;
        do
        {
            var top = boundedRegion.GetEdge(Edge.Top).ToEnvelope();
            var right = boundedRegion.GetEdge(Edge.Right).ToEnvelope();
            var left = boundedRegion.GetEdge(Edge.Left).ToEnvelope();
            var bottom = boundedRegion.GetEdge(Edge.Bottom).ToEnvelope();

            mergeOverlaps =
                Sheet.MergedCells
                     .Search(top, right, left, bottom)
                     .Where(x => !boundedRegion.Contains(x.Region))
                     .ToList();

            // Expand bounded selection to cover all the merges
            foreach (var merge in mergeOverlaps)
            {
                boundedRegion = boundedRegion.GetBoundingRegion(merge.Region);
            }
        } while (mergeOverlaps.Any());

        return boundedRegion;
    }

    /// <summary>
    /// Removes the region from the selection
    /// </summary>
    /// <param name="region"></param>
    public void Remove(IRegion region)
    {
        _regions.Remove(region);
        var newActiveRegion = _regions.LastOrDefault();
        if (newActiveRegion != null)
        {
            ActiveRegion = newActiveRegion;
            ActiveCellPosition = newActiveRegion.Start;
        }

        emitSelectionChange();
    }

    /// <summary>
    /// Clears any selections or active selections and sets the selection to the region specified
    /// </summary>
    /// <param name="region"></param>
    public void SetSingle(IRegion region)
    {
        _regions.Clear();
        this.EndSelecting();
        this.ActiveCellPosition = region.Start;
        this.AddRegionToSelections(region);
    }

    /// <summary>
    /// Sets selection to a single cell and clears any current selections
    /// </summary>
    /// <param name="row"></param> 
    /// <param name="col"></param>
    public void SetSingle(int row, int col)
    {
        SetSingle(new Region(row, col));
    }

    /// <summary>
    /// Returns true if the position is inside any of the active selections
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsSelected(int row, int col)
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

    /// <summary>
    /// Move the active cell position to the next most relevant position
    /// If there's nowhere to go, collapse and move down. Otherwise, move through all regions,
    /// setting them as active where appropriate.
    /// </summary>
    /// <param name="rowDir"></param>
    public void MoveActivePosition(int rowDir)
    {
        if (ActiveRegion == null)
            return;

        // If it's currently inside a merged cell, find where it should next move to
        var merge = Sheet.GetMergedRegionAtPosition(ActiveCellPosition.Row, ActiveCellPosition.Col);
        if (merge != null)
        {
            // Move multiple rows if the current position is on the edge away
            // from the movement direction.
            if (rowDir == 1 && merge.TopLeft.Row == ActiveCellPosition.Row ||
                rowDir == -1 && merge.BottomRight.Row == ActiveCellPosition.Row)
            {
                rowDir *= merge.Height;
            }
        }

        // Fix the active region to surrounds of the sheet
        var activeRegionFixed = ActiveRegion.GetIntersection(_sheet.Region);

        // If the active region is only one cell and there are no other regions,
        // clear the regions and move the whole thing down
        if (_regions.Count == 1 && (activeRegionFixed.Area == 1 || activeRegionFixed.Equals(merge)))
        {
            // We end up with a new region of area 1 (or the size of the merged cell it is not on)
            _regions.Clear();

            var newRowPosn = Math.Max(_sheet.Region.TopLeft.Row,
                                      Math.Min(_sheet.Region.BottomRight.Row, ActiveCellPosition.Row + rowDir));
            var newRegion = new Region(newRowPosn, ActiveCellPosition.Col);
            newRegion = ExpandRegionOverMerged(newRegion) as Region;
            this.ActiveCellPosition = new CellPosition(newRowPosn, ActiveCellPosition.Col);
            this.AddRegionToSelections(newRegion);

            emitSelectionChange();
            return;
        }

        // Move the posn and attempt to bring into either the next region
        // or the next cell in the region
        var newRow = ActiveCellPosition.Row + rowDir;
        var newCol = ActiveCellPosition.Col;
        if (newRow > activeRegionFixed.BottomRight.Row)
        {
            newCol++;
            newRow = activeRegionFixed.TopLeft.Row;
            if (newCol > activeRegionFixed.BottomRight.Col)
            {
                var newActiveRegion = getRegionAfterActive();
                var newActiveRegionFixed = newActiveRegion.GetIntersection(_sheet.Region);
                newCol = newActiveRegionFixed.TopLeft.Col;
                newRow = newActiveRegionFixed.TopLeft.Row;
                ActiveRegion = newActiveRegion;
            }
        }
        else if (newRow < activeRegionFixed.TopLeft.Row)
        {
            newCol--;
            newRow = activeRegionFixed.BottomRight.Row;
            if (newCol < activeRegionFixed.TopLeft.Col)
            {
                var newActiveRegion = getRegionAfterActive();
                var newActiveRegionFixed = newActiveRegion.GetIntersection(_sheet.Region);
                newCol = newActiveRegionFixed.BottomRight.Col;
                newRow = newActiveRegionFixed.BottomRight.Row;
                ActiveRegion = newActiveRegion;
                ActiveRegion = newActiveRegion;
            }
        }

        ActiveCellPosition = new CellPosition(newRow, newCol);
        emitSelectionChange();
    }

    /// <summary>
    /// Sets the active cell position to the position specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void SetActivePosition(int row, int col)
    {
        if (IsEmpty())
            return;
        if (!ActiveRegion.Contains(row, col))
            SetSingle(row, col);
        else // position within active selection
            ActiveCellPosition = new CellPosition(row, col);
    }

    private IRegion getRegionAfterActive()
    {
        var activeRegionIndex = _regions.IndexOf(ActiveRegion!);
        if (activeRegionIndex == -1)
            throw new Exception("No range is active?");
        activeRegionIndex++;
        if (activeRegionIndex >= _regions.Count)
            activeRegionIndex = 0;
        return _regions[activeRegionIndex];
    }

    private IRegion getRegionBeforeActive()
    {
        var activeRegionIndex = _regions.IndexOf(ActiveRegion!);
        if (activeRegionIndex == -1)
            throw new Exception("No range is active?");
        activeRegionIndex--;
        if (activeRegionIndex < 0)
            activeRegionIndex = _regions.Count - 1;
        return _regions[activeRegionIndex];
    }

    private void emitSelectionChange()
    {
        SelectionChanged?.Invoke(this, _regions);
    }

    public IEnumerable<IReadOnlyCell> GetCells()
    {
        return _sheet.GetCellsInRegions(_regions);
    }

    /// <summary>
    /// Returns the position that should receive input
    /// </summary>
    /// <returns></returns>
    public CellPosition GetInputPosition()
    {
        if (ActiveCellPosition.IsInvalid)
            throw new Exception("Invalid cell position");

        // Check whether there are any merged regions at the ActiveCellPosition.
        // If there are, the input position is the top left of the merged,
        // Otherwise it corresponds to the active cell position.
        var merged = Sheet.GetMergedRegionAtPosition(ActiveCellPosition.Row, ActiveCellPosition.Col);
        if (merged == null)
            return ActiveCellPosition;
        else
            return new CellPosition(merged.TopLeft.Row, merged.TopLeft.Col);
    }

    #endregion
}