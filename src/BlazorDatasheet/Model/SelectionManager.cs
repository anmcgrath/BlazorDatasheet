namespace BlazorDatasheet.Model;

public class SelectionManager
{
    private Sheet _sheet;
    public bool IsSelecting => ActiveSelection != null;

    /// <summary>
    /// The selection that is happening but has not been finalised, e.g
    /// intended to be when the user is dragging the mouse across the cells
    /// </summary>
    public Selection? ActiveSelection { get; private set; }

    private List<Selection> _selections;

    /// <summary>
    /// The list of current selections
    /// </summary>
    public IReadOnlyCollection<Selection> Selections => _selections;

    public SelectionManager(Sheet sheet)
    {
        _sheet = sheet;
    }

    /// <summary>
    /// Start selecting at a position (row, col). This selection is not finalised until EndSelecting() is called.
    /// </summary>
    /// <param name="sheet">The sheet it applies to</param>
    /// <param name="row">The row where the selection should start</param>
    /// <param name="col">The col where the selection should start</param>
    public void BeginSelectingCell(int row, int col)
    {
        ActiveSelection = new Selection(new Range(row, col), _sheet);
        emitSelectingChange();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="row"></param>
    public void BeginSelectingRow(int row)
    {
        
    }

    public void BeginSelectingCol(int col)
    {
        
    }

    /// <summary>
    /// Updates the current selecting selection by extending it to row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void UpdateSelectingEndPosition(int row, int col)
    {
        if (!IsSelecting)
            return;

        ActiveSelection.Extend(row, col);
        emitSelectingChange();
    }

    /// <summary>
    /// Ends the selecting process and adds the selection to the stack
    /// </summary>
    public void EndSelecting()
    {
        _selections.Add(ActiveSelection);
        ActiveSelection = null;
        emitSelectingChange();
    }

    /// <summary>
    /// Clears the selecting process and discards the active selecting object
    /// </summary>
    public void CancelSelecting()
    {
        ActiveSelection = null;
        emitSelectingChange();
    }

    /// <summary>
    /// Extends the most recently added selection to the row, col position and makes it the active selection
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void ExtendSelection(int row, int col)
    {
        if (!_selections.Any())
            return;

        ActiveSelection = _selections.Last();
        _selections.RemoveAt(_selections.Count - 1);

        ActiveSelection.Extend(row, col);
        this.emitSelectionChange();
        this.emitSelectingChange();
    }

    /// <summary>
    /// Returns all selections
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Selection> GetSelections()
    {
        return Selections;
    }

    /// <summary>
    /// Determines whether a column contains any cells that are selected or being selected
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsColumnActive(int col)
    {
        if (IsSelecting && ActiveSelection.Range.ContainsCol(col))
            return true;
        return Selections.Any(x => x.Range.ContainsCol(col));
    }
    
    /// <summary>
    /// Determines whether a row contains any cells that are selected or being selected
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool IsRowActive(int row)
    {
        if (IsSelecting && ActiveSelection.Range.ContainsRow(row))
            return true;
        return Selections.Any(x => x.Range.ContainsRow(row));
    }

    /// <summary>
    /// Fired when the active selection (currently being selected) changes
    /// </summary>
    public event Action<Selection?> OnSelectingChange;

    private void emitSelectingChange()
    {
        OnSelectingChange?.Invoke(this.ActiveSelection);
    }

    /// <summary>
    /// Fired when the current selection changes
    /// </summary>
    public event Action<IEnumerable<Selection>> OnSelectionChange;

    private void emitSelectionChange()
    {
        OnSelectionChange?.Invoke(Selections);
    }
}