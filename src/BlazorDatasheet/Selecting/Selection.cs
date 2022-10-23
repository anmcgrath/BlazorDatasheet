using BlazorDatasheet.Data;

namespace BlazorDatasheet.Selecting;

public class Selection
{
    private Sheet _sheet;

    /// <summary>
    /// The regions in the current selection
    /// </summary>
    public IReadOnlyList<IRegion> Regions => _regions;

    private List<IRegion> _regions = new();

    /// <summary>
    /// The region that is active for accepting user input, usually the most recent region added
    /// </summary>
    public IRegion? ActiveRegion { get; private set; }

    /// <summary>
    /// The position of the cell that should take user input. Usually the start position of ActiveRegion
    /// </summary>
    public CellPosition ActiveCellPosition { get; private set; }

    /// <summary>
    /// Fired when the current selection changes
    /// </summary>
    public event EventHandler<IEnumerable<IRegion>> Changed;

    public Selection(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Clears 
    /// </summary>
    public void Clear()
    {
        _regions.Clear();
        ActiveRegion = null;
        emitSelectionChange();
    }

    /// <summary>
    /// Adds the region to the selection
    /// </summary>
    /// <param name="region"></param>
    public void Add(IRegion region)
    {
        _regions.Add(region);
        ActiveRegion = region;
        ActiveCellPosition = ActiveRegion.Start;
        emitSelectionChange();
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
        this.Add(region);
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

    internal void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
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
        // if 
        if (ActiveRegion == null)
            return;

        // Fix the active region to surrounds of the sheet
        var activeRegionFixed = ActiveRegion.GetIntersection(_sheet.Region);

        // If the active region is only one cell and there are no other regions,
        // clear the regions and move the whole thing down
        if (_regions.Count == 1 && activeRegionFixed.Area == 1)
        {
            _regions.Clear();
            var newRegion = new Region(ActiveCellPosition.Row + rowDir, ActiveCellPosition.Col);
            newRegion.Constrain(_sheet.Region);
            this.Add(newRegion);
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

    /// <summary>
    /// Extends the active selection to the position specified
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void ExtendActiveRegion(int row, int col)
    {
        if (ActiveRegion != null)
        {
            ActiveRegion.ExtendTo(row, col);
            emitSelectionChange();
        }
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
        Changed?.Invoke(this, _regions);
    }

    public IEnumerable<Cell> GetCells()
    {
        return _sheet.GetCellsInRegions(_regions);
    }
}