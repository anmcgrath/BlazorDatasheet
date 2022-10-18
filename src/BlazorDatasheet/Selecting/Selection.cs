using BlazorDatasheet.Data;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Selecting;

public class Selection
{
    private Sheet _sheet;

    /// <summary>
    /// The ranges in the current selection
    /// </summary>
    public IReadOnlyList<IFixedSizeRange> Ranges => _ranges;

    private List<IFixedSizeRange> _ranges = new();

    /// <summary>
    /// The range that is active for accepting user input, usually the most recent range added
    /// </summary>
    public IFixedSizeRange? ActiveRange { get; private set; }

    /// <summary>
    /// The position of the cell that should take user input. Usually the start position of ActiveRange
    /// </summary>
    public CellPosition ActiveCellPosition { get; private set; }

    /// <summary>
    /// Fired when the current selection changes
    /// </summary>
    public event EventHandler<IEnumerable<IRange>> Changed;

    public Selection(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Clears 
    /// </summary>
    public void Clear()
    {
        _ranges.Clear();
        emitSelectionChange();
    }

    /// <summary>
    /// Adds the range to the selection
    /// </summary>
    /// <param name="range"></param>
    public void Add(IRange range)
    {
        var fixedRange = range.GetIntersection(_sheet.Range);
        _ranges.Add(fixedRange);
        ActiveRange = fixedRange;
        ActiveCellPosition = ActiveRange.Start;
        emitSelectionChange();
    }

    /// <summary>
    /// Removes the range from the selection
    /// </summary>
    /// <param name="range"></param>
    public void Remove(IFixedSizeRange range)
    {
        _ranges.Remove(range);
        var newActiveRange = _ranges.LastOrDefault();
        if (newActiveRange != null)
        {
            ActiveRange = newActiveRange;
            ActiveCellPosition = newActiveRange.Start;
        }

        emitSelectionChange();
    }

    /// <summary>
    /// Clears any selections or active selections and sets the selection to the range specified
    /// </summary>
    /// <param name="range"></param>
    public void SetSingle(IRange range)
    {
        _ranges.Clear();
        this.Add(range);
    }

    /// <summary>
    /// Sets selection to a single cell and clears any current selections
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void SetSingle(int row, int col)
    {
        SetSingle(new Range(row, col));
    }

    /// <summary>
    /// Returns true if the position is inside any of the active selections
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsSelected(int row, int col)
    {
        if (!_ranges.Any())
            return false;
        return _ranges
            .Any(x => x.Contains(row, col));
    }

    internal void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Returns whether there are no ranges in the selection
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        return !_ranges.Any();
    }

    /// <summary>
    /// Move the active cell position to the next most relevant position
    /// If there's nowhere to go, collapse and move down. Otherwise, move through all ranges,
    /// setting them as active where appropriate.
    /// </summary>
    /// <param name="rowDir"></param>
    public void MoveActivePosition(int rowDir)
    {
        // if 
        if (ActiveRange == null)
            return;

        // Fix the active range to surrounds of the sheet
        var activeRangeFixed = ActiveRange.GetIntersection(_sheet.Range);

        // If the active range is only one cell and there are no other ranges,
        // clear the ranges and move the whole thing down
        if (_ranges.Count == 1 && activeRangeFixed.Area == 1)
        {
            _ranges.Clear();
            var newRange = new Range(ActiveCellPosition.Row + rowDir, ActiveCellPosition.Col);
            newRange.Constrain(_sheet.Range);
            this.Add(newRange);
            emitSelectionChange();
            return;
        }

        // Move the posn and attempt to bring into either the next range
        // or the next cell in the range
        var newRow = ActiveCellPosition.Row + rowDir;
        var newCol = ActiveCellPosition.Col;
        if (newRow > activeRangeFixed.BottomRight.Row)
        {
            newCol++;
            newRow = activeRangeFixed.TopLeft.Row;
            if (newCol > activeRangeFixed.BottomRight.Col)
            {
                ActiveRange = getRangeAfterActive();
                newCol = ActiveRange.TopLeft.Col;
                newRow = ActiveRange.TopLeft.Row;
            }
        }
        else if (newRow < activeRangeFixed.TopLeft.Row)
        {
            newCol--;
            newRow = activeRangeFixed.BottomRight.Row;
            if (newCol < activeRangeFixed.TopLeft.Col)
            {
                ActiveRange = getRangeBeforeActive();
                newCol = ActiveRange.BottomRight.Col;
                newRow = ActiveRange.BottomRight.Row;
            }
        }

        ActiveCellPosition = new CellPosition(newRow, newCol);
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
        if (!ActiveRange.Contains(row, col))
            SetSingle(row, col);
        else // position within active selection
            ActiveCellPosition = new CellPosition(row, col);
    }

    private IFixedSizeRange getRangeAfterActive()
    {
        var activeRangeIndex = _ranges.IndexOf(ActiveRange!);
        if (activeRangeIndex == -1)
            throw new Exception("No range is active?");
        activeRangeIndex++;
        if (activeRangeIndex >= _ranges.Count)
            activeRangeIndex = 0;
        return _ranges[activeRangeIndex];
    }

    private IFixedSizeRange getRangeBeforeActive()
    {
        var activeRangeIndex = _ranges.IndexOf(ActiveRange!);
        if (activeRangeIndex == -1)
            throw new Exception("No range is active?");
        activeRangeIndex--;
        if (activeRangeIndex < 0)
            activeRangeIndex = _ranges.Count - 1;
        return _ranges[activeRangeIndex];
    }

    private void emitSelectionChange()
    {
        Changed?.Invoke(this, _ranges);
    }

    public IEnumerable<Cell> GetCells()
    {
        return _sheet.GetCellsInRanges(_ranges);
    }
}