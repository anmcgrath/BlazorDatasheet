using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events;
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
    public event EventHandler<IEnumerable<IRegion>>? SelectionChanged;

    /// <summary>
    /// Fired when the current selecting region changes
    /// </summary>
    public event EventHandler<IRegion?>? SelectingChanged;

    public event EventHandler<CellsSelectedEventArgs>? CellsSelected;

    public Selection(Sheet sheet)
    {
        _sheet = sheet;
    }

    #region SELECTING

    public void BeginSelectingCell(int row, int col)
    {
        this.SelectingRegion = new Region(row, col);
        this.SelectingStartPosition = new CellPosition(row, col);
        this._selectingMode = SelectionMode.Cell;
        this.SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
        EmitSelectingChanged();
    }

    public void BeginSelectingCol(int col)
    {
        this.SelectingRegion = new ColumnRegion(col, col);
        this.SelectingStartPosition = new CellPosition(0, col);
        this._selectingMode = SelectionMode.Column;
        this.SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
        EmitSelectingChanged();
    }

    public void BeginSelectingRow(int row)
    {
        this.SelectingRegion = new RowRegion(row, row);
        this.SelectingStartPosition = new CellPosition(row, 0);
        this._selectingMode = SelectionMode.Row;
        this.SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
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

        SelectingRegion = ExpandRegionOverMerged(SelectingRegion);
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
        this.ActiveCellPosition = new CellPosition(SelectingStartPosition.row, SelectingStartPosition.col);
        this.Add(SelectingRegion);
        SelectingRegion = null;
        EmitSelectingChanged();
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
        _regions.Clear();
        ActiveRegion = null;
        EmitSelectionChange();
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
        _regions.AddRange(regions);
        ActiveRegion = ExpandRegionOverMerged(regions.LastOrDefault());
        EmitSelectionChange();
    }

    /// <summary>
    /// Expands the <paramref name="region"/> so that it covers any merged cells
    /// </summary>
    private IRegion? ExpandRegionOverMerged(IRegion? region)
    {
        if (region is ColumnRegion || region is RowRegion)
            return region;

        // Look at the four sides of the active region
        // If any of the sides are touching active regions, we check whether the selection
        // covers the region entirely. If not, expand the sides so that they cover.
        // Continue until there are no more merge intersections that we don't fully cover.
        var boundedRegion = region?.Copy();

        if (boundedRegion == null)
            return null;

        List<IRegion> mergeOverlaps;
        do
        {
            var top = boundedRegion.GetEdge(Edge.Top);
            var right = boundedRegion.GetEdge(Edge.Right);
            var left = boundedRegion.GetEdge(Edge.Left);
            var bottom = boundedRegion.GetEdge(Edge.Bottom);

            mergeOverlaps =
                _sheet.Cells
                    .GetMerges(new[] { top, right, left, bottom })
                    .Where(x => !boundedRegion.Contains(x))
                    .ToList();

            // Expand bounded selection to cover all the merges
            foreach (var merge in mergeOverlaps)
            {
                boundedRegion = boundedRegion.GetBoundingRegion(merge);
            }
        } while (mergeOverlaps.Any());

        return boundedRegion;
    }

    /// <summary>
    /// Extends the active region to the row/col specified
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void ExtendTo(int row, int col)
    {
        ActiveRegion?.ExtendTo(row, col);
        EmitSelectingChanged();
    }

    /// <summary>
    /// Clears any selections or active selections and sets the selection to the region specified
    /// </summary>
    /// <param name="region"></param>
    public void Set(IRegion region) => Set(new List<IRegion>() { region });

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
        this.EndSelecting();
        if (regions.Any())
        {
            this.ActiveCellPosition = regions.First().TopLeft;
            this.Add(regions);
        }
    }

    public void ConstrainSelectionToSheet()
    {
        if (_sheet.NumRows == 0 || _sheet.NumCols == 0)
        {
            this.ClearSelections();
            return;
        }

        var constrainedRegions = new List<IRegion>();
        for (int i = 0; i < _regions.Count; i++)
        {
            var intersection = _regions[i].GetIntersection(_sheet.Region);
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

    /// <summary>
    /// Move the active cell position to the next most relevant position
    /// If there's nowhere to go, collapse and move down. Otherwise, move through all regions,
    /// setting them as active where appropriate.
    /// </summary>
    /// <param name="rowDir"></param>
    public void MoveActivePositionByRow(int rowDir)
    {
        if (ActiveRegion == null || rowDir == 0)
            return;

        rowDir = Math.Abs(rowDir) / rowDir;

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
            newRegion = ExpandRegionOverMerged(newRegion) as Region;
            this.ActiveCellPosition = new CellPosition(offsetPosn.row, offsetPosn.col);
            this.Add(newRegion);

            EmitSelectionChange();
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
                newCol = newActiveRegionFixed.Right;
                newRow = newActiveRegionFixed.Bottom;
                ActiveRegion = newActiveRegion;
                ActiveRegion = newActiveRegion;
            }
        }

        ActiveCellPosition = new CellPosition(newRow, newCol);
        EmitSelectionChange();
    }

    /// <summary>
    /// Move the active cell position to the next most relevant position
    /// If there's nowhere to go, collapse and move down. Otherwise, move through all regions,
    /// setting them as active where appropriate.
    /// </summary>
    /// <param name="colDir"></param>
    public void MoveActivePositionByCol(int colDir)
    {
        if (ActiveRegion == null || colDir == 0)
            return;

        colDir = Math.Abs(colDir) / colDir;

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

        // Fix the active region to surrounds of the sheet
        var activeRegionFixed = ActiveRegion.GetIntersection(_sheet.Region);

        // If the active region is only one cell and there are no other regions,
        // clear the regions and move the whole thing down
        if (_regions.Count == 1 && (activeRegionFixed.Area == 1 || activeRegionFixed.Equals(merge)))
        {
            // We end up with a new region of area 1 (or the size of the merged cell it is not on)
            _regions.Clear();

            var offsetPosn = new CellPosition(currRow, currCol);
            offsetPosn = _sheet.Region.GetConstrained(offsetPosn);
            var newRegion = new Region(offsetPosn.row, offsetPosn.col);
            newRegion = ExpandRegionOverMerged(newRegion) as Region;
            this.ActiveCellPosition = new CellPosition(offsetPosn.row, offsetPosn.col);
            this.Add(newRegion);

            EmitSelectionChange();
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
                newCol = newActiveRegionFixed.Right;
                newRow = newActiveRegionFixed.Bottom;
                ActiveRegion = newActiveRegion;
                ActiveRegion = newActiveRegion;
            }
        }

        ActiveCellPosition = new CellPosition(newRow, newCol);
        EmitSelectionChange();
    }

    /// <summary>
    /// Sets the active cell position to the position specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal void SetActivePosition(int row, int col)
    {
        if (IsEmpty())
            return;
        if (!ActiveRegion.Contains(row, col))
            Set(row, col);
        else // position within active selection
            ActiveCellPosition = new CellPosition(row, col);
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

    private void EmitSelectionChange()
    {
        SelectionChanged?.Invoke(this, _regions);
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
        set { _sheet.Cells.SetValues(Ranges.SelectMany(x => x.Positions).Select(x => (x.row, x.col, value))); }
    }

    public void Clear()
    {
        _sheet.Cells.ClearCells(this.Regions);
    }
}