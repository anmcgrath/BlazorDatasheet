namespace BlazorDatasheet.Model;

public class Sheet
{
    public int Rows { get; private set; }
    public int Cols { get; private set; }
    public Cell[,] Cells { get; set; }
    public Stack<Range> Selection { get; private set; }

    /// <summary>
    /// The actively selecting range
    /// </summary>
    public Range ActiveSelecting { get; set; }

    public bool IsSelecting { get; set; }

    public Sheet(int rows, int cols, Cell[,] cells)
    {
        Rows = rows;
        Cols = cols;
        Selection = new Stack<Range>();
        Cells = cells;
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
        var range = new Range()
        {
            ColEnd = col,
            ColStart = col,
            RowEnd = row,
            RowStart = row
        };
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

    public void BeginSelecting(int row, int col, bool clearSelection)
    {
        if (clearSelection)
            ClearSelection();
        ActiveSelecting = new Range()
        {
            RowStart = row,
            RowEnd = row,
            ColStart = col,
            ColEnd = col
        };
        IsSelecting = true;
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
}