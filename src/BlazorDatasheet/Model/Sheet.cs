namespace BlazorDatasheet.Model;

public class Sheet
{
    public int Rows { get; private set; }
    public int Cols { get; private set; }
    public Cell[,] Cells { get; set; }

    public List<Heading> ColumnHeadings { get; private set; }
    public List<Heading> RowHeadings { get; private set; }

    public Dictionary<string, ConditionalFormat> ConditionalFormats { get; set; }
    public Stack<Range> Selection { get; private set; }

    /// <summary>
    /// The actively selecting range
    /// </summary>
    public Range ActiveSelecting { get; set; }

    public bool IsSelecting { get; set; }
    public SelectionMode SelectionMode { get; set; }

    public Sheet(int rows, int cols, Cell[,] cells)
    {
        Rows = rows;
        Cols = cols;
        Selection = new Stack<Range>();
        ConditionalFormats = new Dictionary<string, ConditionalFormat>();
        ColumnHeadings = new List<Heading>();
        RowHeadings = new List<Heading>();
        Cells = cells;
    }

    private Cell[] GetCellsInRange(Range range)
    {
        List<Cell> cells = new List<Cell>();
        var rowStart = Math.Max(0, range.RowStart);
        var rowEnd = Math.Min(Rows - 1, range.RowEnd);
        var colStart = Math.Max(0, range.ColStart);
        var colEnd = Math.Min(Cols - 1, range.ColEnd);
        for (int row = rowStart; row <= rowEnd; row++)
        {
            for (int col = colStart; col <= colEnd; col++)
            {
                if (range.Contains(row, col))
                    cells.Add(Cells[row, col]);
            }
        }

        return cells.ToArray();
    }

    public void RegisterConditionalFormat(string key, ConditionalFormat conditionalFormat)
    {
        this.ConditionalFormats.Add(key, conditionalFormat);
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
        if (row < 0 || row >= Rows)
            return null;
        if (col < 0 || col >= Cols)
            return null;
        return Cells[row, col];
    }

    public void SetCell(int row, int col, Cell value)
    {
        if (row < 0 || row >= Rows)
            return;
        if (col < 0 || col >= Cols)
            return;
        Cells[row, col] = value;
    }

    public void SetSelectionSingle(int row, int col)
    {
        this.Selection.Clear();
        var range = new Range(row, col);
        range.Constrain(Rows, Cols);
        this.Selection.Push(range);
    }

    public CellPosition GetInputForSelection()
    {
        if (IsSelecting)
            return null;
        if (Selection.Count == 0)
            return null;
        var selection = Selection.Last();
        return new CellPosition()
        {
            Row = selection.RowStart,
            Col = selection.ColStart
        };
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

    public void ClearSelection()
    {
        EndSelecting();
        Selection.Clear();
    }

    public bool IsSelected(int row, int col)
    {
        if (IsSelecting && ActiveSelecting.Contains(row, col))
            return true;
        if (!Selection.Any())
            return false;
        return Selection
            .Any(x => x.Contains(row, col));
    }

    public void BeginSelecting(int row, int col, bool clearSelection, SelectionMode mode)
    {
        if (clearSelection)
            ClearSelection();
        ActiveSelecting = new Range(row, col);
        IsSelecting = true;
        SelectionMode = mode;
    }

    public void EndSelecting()
    {
        IsSelecting = false;
        Selection.Push(ActiveSelecting);
        ActiveSelecting = null;
    }

    public void UpdateSelecting(int row, int col)
    {
        if (!IsSelecting)
            return;
        ActiveSelecting.ColEnd = col;
        ActiveSelecting.RowEnd = row;
    }

    public void ExtendSelection(int row, int col)
    {
        ActiveSelecting = Selection.Pop();
        IsSelecting = true;
        UpdateSelecting(row, col);
    }

    public bool IsColumnActive(int col)
    {
        if (IsSelecting && ActiveSelecting.ContainsCol(col))
            return true;
        return Selection.Any(x => x.ContainsCol(col));
    }

    public bool IsRowActive(int row)
    {
        if (IsSelecting && ActiveSelecting.ContainsRow(row))
            return true;
        return Selection.Any(x => x.ContainsRow(row));
    }

    public void ApplyConditionalFormat(string key, Range range)
    {
        if (!ConditionalFormats.ContainsKey(key))
            return;
        var cf = ConditionalFormats[key];
        cf.AddRange(range);
        var cells = this.GetCellsInRanges(cf.Ranges.ToList());
        foreach (var cell in cells)
            cell.AddConditionalFormat(key);
    }

    public Format GetFormat(Cell cell)
    {
        if (!cell.ConditionalFormattingIds.Any())
            return cell.Formatting;

        var format = cell.Formatting.Clone();
        foreach (var id in cell.ConditionalFormattingIds)
        {
            if (!ConditionalFormats.ContainsKey(id))
                continue;
            var cf = this.ConditionalFormats[id];
            var apply = cf.Rule.Invoke(cell, this.GetCellsInRanges(cf.Ranges.ToList()));
            if (apply)
                format.Merge(cf.Formatting);
        }

        return format;
    }
}