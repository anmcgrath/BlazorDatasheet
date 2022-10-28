using System.Text;
using BlazorDatasheet.Commands;
using BlazorDatasheet.Data.Events;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formats;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Selecting;

namespace BlazorDatasheet.Data;

public class Sheet
{
    /// <summary>
    /// The displayed number of rows in the sheet
    /// </summary>
    public int NumRows { get; private set; }

    /// <summary>
    /// The number of columns in the sheet
    /// </summary>
    public int NumCols { get; private set; }

    /// <summary>
    /// The sheet's headings
    /// </summary>
    public List<Heading> ColumnHeadings { get; private set; }

    /// <summary>
    /// The sheet's row headings
    /// </summary>
    public List<Heading> RowHeadings { get; private set; }

    /// <summary>
    /// Whether to show the row headings
    /// </summary>
    public bool ShowRowHeadings { get; set; } = true;

    /// <summary>
    /// Whether to show the column headings. Default is true.
    /// </summary>
    public bool ShowColumnHeadings { get; set; } = true;

    /// <summary>
    /// Managers commands & undo/redo. Default is true.
    /// </summary>
    public CommandManager Commands { get; private set; }

    /// <summary>
    /// The bounds of the sheet
    /// </summary>
    public Region? Region => new Region(0, NumRows - 1, 0, NumCols - 1);

    /// <summary>
    /// Provides functions for managing the sheet's conditional formatting
    /// </summary>
    public ConditionalFormatManager ConditionalFormatting { get; }

    /// <summary>
    /// The sheet's active selection
    /// </summary>
    public Selection Selection { get; }

    private readonly Dictionary<string, Type> _editorTypes;
    public IReadOnlyDictionary<string, Type> EditorTypes => _editorTypes;
    private Dictionary<string, Type> _renderComponentTypes { get; set; }
    public IReadOnlyDictionary<string, Type> RenderComponentTypes => _renderComponentTypes;

    /// <summary>
    /// Fired when a row is inserted into the sheet
    /// </summary>
    public event EventHandler<RowInsertedEventArgs> RowInserted;

    /// <summary>
    /// Fired when a row is removed from the sheet.
    /// </summary>
    public event EventHandler<RowRemovedEventArgs> RowRemoved;

    /// <summary>
    /// Fired when one or more cells are changed
    /// </summary>
    public event EventHandler<IEnumerable<ChangeEventArgs>> CellsChanged;

    private IMatrixDataStore<Cell> _cellDataStore { get; set; } = new SparseMatrixStore<Cell>();

    private Sheet()
    {
        ColumnHeadings = new List<Heading>();
        RowHeadings = new List<Heading>();
        Commands = new CommandManager(this);
        Selection = new Selection(this);
        ConditionalFormatting = new ConditionalFormatManager(this);
        _editorTypes = new Dictionary<string, Type>();
        _renderComponentTypes = new Dictionary<string, Type>();

        registerDefaultEditors();
        registerDefaultRenderers();
    }

    public Sheet(int numRows, int numCols, Cell[,] cells) : this()
    {
        NumCols = numCols;
        NumRows = numRows;

        for (var i = 0; i < numRows; i++)
        {
            for (int j = 0; j < NumCols; j++)
            {
                var cell = cells[i, j];
                cell.Row = i;
                cell.Col = j;
                _cellDataStore.Set(i, j, cell);
            }
        }
    }

    public Sheet(int numRows, int numCols) : this()
    {
        NumCols = numCols;
        NumRows = numRows;
    }

    /// <summary>
    /// Inserts a row at an index specified.
    /// </summary>
    /// <param name="rowIndex">The index that the new row will be at. If the index is outside of the region of rows,
    /// a row will be either inserted at the start or appended at the end</param>
    public void InsertRowAt(int rowIndex)
    {
        var cmd = new InsertRowAtCommand(rowIndex);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// The internal insert function that does all the work of adding a row.
    /// This function does not add a command that is able to be undone.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    internal bool InsertRowAtImpl(int rowIndex)
    {
        _cellDataStore.InsertRowAt(rowIndex);
        NumRows++;
        RowInserted?.Invoke(this, new RowInsertedEventArgs(rowIndex + 1));
        return true;
    }

    internal bool RemoveRowAtImpl(int index)
    {
        if (index >= 0 && index < NumRows)
        {
            _cellDataStore.RemoveRowAt(index);
            NumRows--;
            RowRemoved?.Invoke(this, new RowRemovedEventArgs(index));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns a single cell range at the position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public BRangeCell Range(int row, int col)
    {
        return new BRangeCell(this, row, col);
    }

    /// <summary>
    /// Returns a range with the positions specified
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <returns></returns>
    public BRange Range(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        return Range(new Region(rowStart, rowEnd, colStart, colEnd));
    }

    /// <summary>
    /// Returns a new range that contains the region specified
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public BRange Range(IRegion region)
    {
        return Range(new List<IRegion>() { region });
    }

    /// <summary>
    /// Returns a new range that contains all the regions specified
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public BRange Range(IEnumerable<IRegion> regions)
    {
        return new BRange(this, regions);
    }


    /// <summary>
    /// Returns all cells in the specified region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegion(IRegion region)
    {
        return (new BRange(this, region))
               .Positions
               .Select(x => this.GetCell(x.row, x.col));
    }

    /// <summary>
    /// Returns all cells that are present in the regions given.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public IEnumerable<IReadOnlyCell> GetCellsInRegions(IEnumerable<IRegion> regions)
    {
        var cells = new List<IReadOnlyCell>();
        foreach (var region in regions)
            cells.AddRange(GetCellsInRegion(region));
        return cells.ToArray();
    }

    /// <summary>
    /// Returns the cell at the specified position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(int row, int col)
    {
        var cell = _cellDataStore.Get(row, col);

        if (cell == null)
            return new Cell(null)
            {
                Row = row,
                Col = col
            };

        cell.Row = row;
        cell.Col = col;
        return cell;
    }

    /// <summary>
    /// Returns the cell at the specified position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public IReadOnlyCell GetCell(CellPosition position)
    {
        return GetCell(position.Row, position.Col);
    }

    internal IEnumerable<(int row, int col)> GetNonEmptyCellPositions(IRegion region)
    {
        return _cellDataStore.GetNonEmptyPositions(region.TopLeft.Row,
                                                   region.BottomRight.Row,
                                                   region.TopLeft.Col,
                                                   region.BottomRight.Col);
    }

    /// <summary>
    /// Registers a cell editor component with a unique name.
    /// If the editor already exists, it will override the existing.
    /// </summary>
    /// <param name="name">A unique name for the editor</param>
    /// <typeparam name="T"></typeparam>
    public void RegisterEditor<T>(string name) where T : ICellEditor
    {
        if (!_editorTypes.ContainsKey(name))
            _editorTypes.Add(name, typeof(T));
        _editorTypes[name] = typeof(T);
    }

    /// <summary>
    /// Registers a cell renderer component with a unique name.
    /// If the renderer already exists, it will override the existing.
    /// </summary>
    /// <param name="name">A unique name for the renderer</param>
    /// <typeparam name="T"></typeparam>
    public void RegisterRenderer<T>(string name) where T : BaseRenderer
    {
        if (!_renderComponentTypes.TryAdd(name, typeof(T)))
            _renderComponentTypes[name] = typeof(T);
    }

    private void registerDefaultEditors()
    {
        RegisterEditor<TextEditorComponent>("text");
        RegisterEditor<DateTimeEditorComponent>("datetime");
        RegisterEditor<BoolEditorComponent>("boolean");
        RegisterEditor<SelectEditorComponent>("select");
    }

    private void registerDefaultRenderers()
    {
        RegisterRenderer<TextRenderer>("text");
        RegisterRenderer<SelectRenderer>("select");
        RegisterRenderer<NumberRenderer>("number");
        RegisterRenderer<BoolRenderer>("boolean");
    }

    /// <summary>
    /// Inserts delimited text from the given position
    /// And assigns cell's values based on the delimited text (tabs and newlines)
    /// Returns the region of cells that surrounds all cells that are affected
    /// </summary>
    /// <param name="text">The text to insert</param>
    /// <param name="inputPosition">The position where the insertion starts</param>
    /// <returns>The region of cells that were affected</returns>
    public Region InsertDelimitedText(string text, CellPosition inputPosition, string newLineDelim = "\n")
    {
        if (inputPosition.IsInvalid)
            return null;

        var lines = text.TrimEnd('\n').Split(newLineDelim);

        // We may reach the end of the sheet, so we only need to paste the rows up until the end.
        var endRow = Math.Min(inputPosition.Row + lines.Length - 1, NumRows - 1);
        // Keep track of the maximum end column that we are inserting into
        // This is used to determine the region to return.
        // It is possible that each line is of different cell lengths, so we return the max for all lines
        var maxEndCol = -1;

        var valChanges = new List<ValueChange>();

        int lineNo = 0;
        for (int row = inputPosition.Row; row <= endRow; row++)
        {
            var lineSplit = lines[lineNo].Split('\t');
            // Same thing as above with the number of columns
            var endCol = Math.Min(inputPosition.Col + lineSplit.Length - 1, NumCols - 1);

            maxEndCol = Math.Max(endCol, maxEndCol);

            int cellIndex = 0;
            for (int col = inputPosition.Col; col <= endCol; col++)
            {
                var cell = this.GetCell(row, col);
                valChanges.Add(new ValueChange(row, col, lineSplit[cellIndex]));
                cellIndex++;
            }

            lineNo++;
        }

        this.SetCellValues(valChanges);

        return new Region(inputPosition.Row, endRow, inputPosition.Col, maxEndCol);
    }

    public bool TrySetCellValue(int row, int col, object value)
    {
        var cmd = new SetCellValueCommand(row, col, value);
        return Commands.ExecuteCommand(cmd);
    }

    internal bool TrySetCellValueImpl(int row, int col, object value, bool raiseEvent = true)
    {
        var cell = _cellDataStore.Get(row, col);
        if (cell == null)
        {
            cell = new Cell(value);
            _cellDataStore.Set(row, col, cell);
            if (raiseEvent)
                CellsChanged?.Invoke(this, new List<ChangeEventArgs>() { new(row, col, null, value) });
            return true;
        }

        // Perform data validation
        var isValid = true;
        foreach (var validator in cell.Validators)
        {
            if (validator.IsValid(value)) continue;
            if (validator.IsStrict)
                return false;
            isValid = false;
        }

        cell.IsValid = isValid;

        // Try to set the cell's value to the new value
        var oldValue = cell.GetValue();
        var setValue = cell.TrySetValue(value);
        if (setValue && raiseEvent)
        {
            var args = new ChangeEventArgs[1]
            {
                new ChangeEventArgs(row, col, oldValue, value)
            };
            CellsChanged?.Invoke(this, args);
        }

        return setValue;
    }

    internal void SetCell(int row, int col, Cell cell)
    {
        _cellDataStore.Set(row, col, cell);
    }

    /// <summary>
    /// Gets the cell's value at row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public object? GetValue(int row, int col)
    {
        return GetCell(row, col)?.GetValue();
    }

    public bool SetCellValues(IEnumerable<ValueChange> changes)
    {
        var cmd = new SetCellValuesCommand(changes);
        return Commands.ExecuteCommand(cmd);
    }

    internal bool SetCellValuesImpl(IEnumerable<ValueChange> changes)
    {
        var changeEvents = new List<ChangeEventArgs>();
        foreach (var change in changes)
        {
            var currValue = GetValue(change.Row, change.Col);
            var set = TrySetCellValueImpl(change.Row, change.Col, change.Value, false);
            var newValue = GetValue(change.Row, change.Col);
            if (set && currValue != newValue)
                changeEvents.Add(new ChangeEventArgs(change.Row, change.Col, currValue, newValue));
        }

        CellsChanged?.Invoke(this, changeEvents);
        return changeEvents.Any();
    }

    /// <summary>
    /// Clears all cell values in the region
    /// </summary>
    /// <param name="region"></param>
    public void ClearCells(BRange range)
    {
        var cmd = new ClearCellsCommand(range);
        Commands.ExecuteCommand(cmd);
    }

    internal void ClearCelllsImpl(BRange range)
    {
        var cells = range.GetNonEmptyCells().Cast<Cell>();
        var changedArgs = new List<ChangeEventArgs>();
        foreach (var cell in cells)
        {
            var row = cell.Row;
            var col = cell.Col;
            var oldVal = cell.GetValue();
            cell.Clear();
            var newVal = cell.GetValue();
            if (oldVal != newVal)
                changedArgs.Add(new ChangeEventArgs(row, col, oldVal, newVal));
        }

        this.CellsChanged?.Invoke(this, changedArgs);
    }

    public string GetRegionAsDelimitedText(IRegion inputRegion, char tabDelimiter = '\t', string newLineDelim = "\n")
    {
        if (inputRegion == null)
            return string.Empty;

        var intersection = inputRegion.GetIntersection(this.Region);
        if (intersection == null)
            return null;

        var range = intersection.Copy();

        var strBuilder = new StringBuilder();

        var r0 = range.TopLeft.Row;
        var r1 = range.BottomRight.Row;
        var c0 = range.TopLeft.Col;
        var c1 = range.BottomRight.Col;

        for (int row = r0; row <= r1; row++)
        {
            for (int col = c0; col <= c1; col++)
            {
                var cell = this.GetCell(row, col);
                var value = cell.GetValue();
                if (value == null)
                    strBuilder.Append("");
                else
                    strBuilder.Append(value.ToString());
                if (col != c1)
                    strBuilder.Append(tabDelimiter);
            }

            strBuilder.Append(newLineDelim);
        }

        return strBuilder.ToString();
    }
}