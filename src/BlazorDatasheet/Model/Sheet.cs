namespace BlazorDatasheet.Model;

public class Sheet
{
    public int NumRows { get; private set; }
    public int NumCols { get; private set; }
    public List<Row> Rows { get; set; }
    public List<Heading> ColumnHeadings { get; private set; }
    public List<Heading> RowHeadings { get; private set; }
    private readonly Dictionary<string, ConditionalFormat> _conditionalFormats;
    internal IReadOnlyDictionary<string, ConditionalFormat> ConditionalFormats => _conditionalFormats;
    public Range Range => new Range(0, NumRows, 0, NumCols);
    private Stack<Range> Selection { get; set; }

    /// <summary>
    /// The actively selecting range
    /// </summary>
    public Range ActiveSelecting { get; private set; }

    public bool IsSelecting { get; private set; }
    public SelectionMode SelectionMode { get; set; }

    public Sheet(int numRows, int numCols, Cell[,] cells)
    {
        NumRows = numRows;
        NumCols = numCols;
        Selection = new Stack<Range>();
        _conditionalFormats = new Dictionary<string, ConditionalFormat>();
        ColumnHeadings = new List<Heading>();
        RowHeadings = new List<Heading>();
        _conditionalFormats = new Dictionary<string, ConditionalFormat>();

        Rows = new List<Row>();
        for (var i = 0; i < NumRows; i++)
        {
            var rowCells = new List<Cell>();
            for (int j = 0; j < NumCols; j++)
            {
                // Perform conditional formatting when setting these cells initially
                rowCells.Add(cells[i, j]);
            }

            var newRow = new Row(rowCells, i);
            Rows.Add(newRow);
        }
    }

    private Cell[] GetCellsInRange(Range range)
    {
        List<Cell> cells = new List<Cell>();
        var rowStart = Math.Max(0, range.RowStart);
        var rowEnd = Math.Min(NumRows - 1, range.RowEnd);
        var colStart = Math.Max(0, range.ColStart);
        var colEnd = Math.Min(NumCols - 1, range.ColEnd);
        for (int row = rowStart; row <= rowEnd; row++)
        {
            for (int col = colStart; col <= colEnd; col++)
            {
                if (range.Contains(row, col))
                    cells.Add(GetCell(row, col));
            }
        }

        return cells.ToArray();
    }

    /// <summary>
    /// Adds a conditional formatting object to the sheet. Must be applied by setting ApplyConditionalFormat
    /// </summary>
    /// <param name="key">A unique ID identifying the conditional format</param>
    /// <param name="conditionalFormat"></param>
    public void RegisterConditionalFormat(string key, ConditionalFormat conditionalFormat)
    {
        this._conditionalFormats.Add(key, conditionalFormat);
    }

    public Cell[] GetCellsInRanges(List<Range> ranges)
    {
        var cells = new List<Cell>();
        foreach (var range in ranges)
            cells.AddRange(GetCellsInRange(range));
        return cells.ToArray();
    }

    public Cell GetCell(int row, int col)
    {
        if (row < 0 || row >= NumRows)
            return null;
        if (col < 0 || col >= NumCols)
            return null;
        return Rows[row].Cells[col];
    }

    public Cell GetCell(CellPosition position)
    {
        return GetCell(position.Row, position.Col);
    }

    public void SetCell(int row, int col, Cell value)
    {
        if (row < 0 || row >= NumRows)
            return;
        if (col < 0 || col >= NumCols)
            return;
        Rows[row].Cells[col] = value;
    }

    /// <summary>
    /// Set the selection to a single cell & clear all current selections
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void SetSelectionSingle(int row, int col)
    {
        this.Selection.Clear();
        var range = new Range(row, col);
        range.Constrain(NumRows, NumCols);
        this.Selection.Push(range);
    }

    /// <summary>
    /// Determines the most appropriate position for inputting new data, based on the current selections
    /// </summary>
    /// <returns>The most appropriate position for inputting new data</returns>
    public CellPosition GetInputForSelection()
    {
        if (IsSelecting)
            return null;
        if (Selection.Count == 0)
            return null;
        var selection = Selection.Last();
        return new CellPosition(selection.RowStart, selection.ColStart);
    }

    public void MoveSelection(int drow, int dcol)
    {
        if (IsSelecting)
            return;

        var recentSel = Selection.LastOrDefault();
        if (recentSel == null)
            return;

        SetSelectionSingle(recentSel.RowStart + drow, recentSel.ColStart + dcol);
    }

    /// <summary>
    /// Clears all current selections
    /// </summary>
    public void ClearSelection()
    {
        EndSelecting();
        Selection.Clear();
    }

    /// <summary>
    /// Returns true if the position is inside any of the active selections
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsSelected(int row, int col)
    {
        if (IsSelecting && ActiveSelecting.Contains(row, col))
            return true;
        if (!Selection.Any())
            return false;
        return Selection
            .Any(x => x.Contains(row, col));
    }

    /// <summary>
    /// Starts a new "Selecting" process
    /// </summary>
    /// <param name="row">The row to start selecting at</param>
    /// <param name="col">The col to start selecting at</param>
    /// <param name="clearSelection">Whether to clear all current selections</param>
    /// <param name="mode">The way the selection is triggered</param>
    public void BeginSelecting(int row, int col, bool clearSelection, SelectionMode mode)
    {
        if (clearSelection)
            ClearSelection();
        ActiveSelecting = new Range(row, col);
        IsSelecting = true;
        SelectionMode = mode;
    }

    /// <summary>
    /// Ends the current "Selecting" process and adds the new selection to the stack
    /// </summary>
    public void EndSelecting()
    {
        IsSelecting = false;
        Selection.Push(ActiveSelecting);
        ActiveSelecting = null;
    }

    /// <summary>
    /// Cancels the current "Selecting" process and does not add the new selection to the stack
    /// </summary>
    public void CancelSelecting()
    {
        IsSelecting = false;
        ActiveSelecting = null;
    }

    /// <summary>
    /// Updates the current "Selecting" process by extending it to row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void UpdateSelectingEndPosition(int row, int col)
    {
        if (!IsSelecting)
            return;
        ActiveSelecting.ColEnd = col;
        ActiveSelecting.RowEnd = row;
    }

    /// <summary>
    /// Extends the most recently added selection to the row, col position
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void ExtendSelection(int row, int col)
    {
        ActiveSelecting = Selection.Pop();
        IsSelecting = true;
        UpdateSelectingEndPosition(row, col);
    }

    /// <summary>
    /// Determines whether a column contains any cells that are selected or being selected
    /// </summary>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsColumnActive(int col)
    {
        if (IsSelecting && ActiveSelecting.ContainsCol(col))
            return true;
        return Selection.Any(x => x.ContainsCol(col));
    }

    /// <summary>
    /// Determines whether a row contains any cells that are selected or being selected
    /// </summary>
    /// <param name="row"></param>
    /// <returns></returns>
    public bool IsRowActive(int row)
    {
        if (IsSelecting && ActiveSelecting.ContainsRow(row))
            return true;
        return Selection.Any(x => x.ContainsRow(row));
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to all cells in a range, if the conditional formatting exists.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void ApplyConditionalFormat(string key, Range range)
    {
        if (!ConditionalFormats.ContainsKey(key))
            return;
        var cf = ConditionalFormats[key];
        cf.AddRange(range);
        var cells = this.GetCellsInRanges(cf.Ranges.ToList());
        foreach (var cell in cells)
        {
            cell.AddConditionalFormat(key);
        }
    }

    /// <summary>
    /// Applies the conditional format specified by "key" to a particular cell. If setting the format to a number of cells,
    /// prefer setting via a range.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="range"></param>
    public void ApplyConditionalFormat(string key, int row, int col)
    {
        ApplyConditionalFormat(key, new Range(row, col));
    }

    /// <summary>
    /// Determines the "final" formatting of a cell by applying any conditional formatting
    /// </summary>
    /// <param name="cell"></param>
    /// <returns></returns>
    public Format GetFormat(Cell cell)
    {
        if (!cell.ConditionalFormattingIds.Any())
            return cell.Formatting;

        var format = cell.Formatting.Clone();
        foreach (var id in cell.ConditionalFormattingIds)
        {
            if (!ConditionalFormats.ContainsKey(id))
                continue;
            var conditionalFormat = this.ConditionalFormats[id];
            var cellsWithConditionalFormat = this.GetCellsInRanges(conditionalFormat.Ranges.ToList());
            var apply = conditionalFormat.Rule.Invoke(cell, cellsWithConditionalFormat);
            if (apply)
                format.Merge(conditionalFormat.FormatFunc.Invoke(cell, cellsWithConditionalFormat));
        }

        return format;
    }
}