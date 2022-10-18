using System.Text;
using BlazorDatasheet.Commands;
using BlazorDatasheet.Data.Events;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formats;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Selecting;

namespace BlazorDatasheet.Data;

public class Sheet
{
    /// <summary>
    /// The number of rows in the sheet
    /// </summary>
    public int NumRows => _rows.Count;

    /// <summary>
    /// The number of columns in the sheet
    /// </summary>
    public int NumCols { get; private set; }

    /// <summary>
    /// The sheet's rows. Private so that the sheet manages when rows are added or subtracted.
    /// </summary>
    private readonly List<Row> _rows = new();

    /// <summary>
    /// The sheet's rows
    /// </summary>
    public IReadOnlyList<Row> Rows => _rows;

    /// <summary>
    /// Using blazor virtualisation requires an ICollection to render so here it is.
    /// </summary>
    internal ICollection<Row> RowCollection => _rows;

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
    public Range? Range => new Range(0, NumRows - 1, 0, NumCols - 1);

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

        for (var i = 0; i < numRows; i++)
        {
            var rowCells = new List<Cell>();
            for (int j = 0; j < NumCols; j++)
            {
                rowCells.Add(cells[i, j]);
                cells[i, j].Row = i;
                cells[i, j].Col = j;
            }

            var newRow = new Row(rowCells, i);
            _rows.Add(newRow);
        }
    }

    public Sheet(int numRows, int numCols) : this()
    {
        NumCols = numCols;
        for (int i = 0; i < numRows; i++)
        {
            var cells = new List<Cell>();
            for (int j = 0; j < numCols; j++)
            {
                var cell = new Cell()
                {
                    Col = j,
                    Row = i
                };
                cells.Add(cell);
            }

            var row = new Row(cells, i);
            _rows.Add(row);
        }
    }

    /// <summary>
    /// Inserts a row to the end of the sheet
    /// </summary>
    public void InsertRow(Row? row = null)
    {
        InsertRowAt(_rows.Count, row);
    }

    /// <summary>
    /// Inserts a row at an index specified.
    /// </summary>
    /// <param name="rowIndex">The index that the new row will be at. If the index is outside of the range of rows,
    /// a row will be either inserted at the start or appended at the end</param>
    public void InsertRowAt(int rowIndex, Row? row = null)
    {
        var cmd = new InsertRowAtCommand(rowIndex, row);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// The internal insert function that does all the work of adding a row.
    /// This function does not add a command that is able to be undone.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    internal bool InsertRowAtImpl(int rowIndex, Row? row = null)
    {
        if (row == null)
        {
            var cells = new List<Cell>();
            for (int i = 0; i < NumCols; i++)
            {
                cells.Add(new Cell() { Col = i, Row = rowIndex });
            }

            row = new Row(cells, rowIndex);
        }

        _rows.Insert(rowIndex, row);
        updateRowIndices(rowIndex);
        RowInserted?.Invoke(this, new RowInsertedEventArgs(rowIndex));
        return true;
    }

    internal bool RemoveRowAtImpl(int index)
    {
        if (index >= 0 && index < _rows.Count)
        {
            _rows.RemoveAt(index);
            updateRowIndices(index);
            RowRemoved?.Invoke(this, new RowRemovedEventArgs(index));
            return true;
        }

        return false;
    }

    private IEnumerable<Cell> GetCellsInRange(IRange range)
    {
        var fixedRange = range.GetIntersection(this.Range);
        List<Cell> cells = new List<Cell>();
        foreach (var position in fixedRange)
        {
            cells.Add(this.GetCell(position.Row, position.Col));
        }

        return cells.ToArray();
    }

    /// <summary>
    /// Returns all cells that are present in the ranges given.
    /// </summary>
    /// <param name="ranges"></param>
    /// <returns></returns>
    public IEnumerable<Cell> GetCellsInRanges(IEnumerable<IRange> ranges)
    {
        var cells = new List<Cell>();
        foreach (var range in ranges)
            cells.AddRange(GetCellsInRange(range));
        return cells.ToArray();
    }

    /// <summary>
    /// Returns the cell at the specified position.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public Cell GetCell(int row, int col)
    {
        if (row < 0 || row >= NumRows)
            return null;
        if (col < 0 || col >= NumCols)
            return null;

        return Rows[row].Cells[col];
    }

    /// <summary>
    /// Returns the cell at the specified position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Cell GetCell(CellPosition position)
    {
        return GetCell(position.Row, position.Col);
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
    /// Returns the range of cells that surrounds all cells that are affected
    /// </summary>
    /// <param name="text">The text to insert</param>
    /// <param name="inputPosition">The position where the insertion starts</param>
    /// <returns>The range of cells that were affected</returns>
    internal Range InsertDelimitedText(string text, CellPosition inputPosition)
    {
        if (inputPosition.InvalidPosition)
            return null;

        var lines = text.Split(Environment.NewLine);

        // We may reach the end of the sheet, so we only need to paste the rows up until the end.
        var endRow = Math.Min(inputPosition.Row + lines.Length - 1, NumRows - 1);
        // Keep track of the maximum end column that we are inserting into
        // This is used to determine the range to return.
        // It is possible that each line is of different cell lengths, so we return the max for all lines
        var maxEndCol = -1;

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
                TrySetCellValue(row, col, lineSplit[cellIndex]);
                cellIndex++;
            }

            lineNo++;
        }

        return new Range(inputPosition.Row, endRow, inputPosition.Col, maxEndCol);
    }

    public bool TrySetCellValue(int row, int col, object value)
    {
        var cmd = new SetCellValueCommand(row, col, value);
        return Commands.ExecuteCommand(cmd);
    }

    internal bool TrySetCellValueImpl(int row, int col, object value)
    {
        var cell = this.GetCell(row, col);
        if (cell == null)
            return false;

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
        if (setValue)
        {
            var args = new ChangeEventArgs[1]
            {
                new ChangeEventArgs(row, col, oldValue, value)
            };
            CellsChanged?.Invoke(this, args);
        }

        return setValue;
    }

    /// <summary>
    /// Clears all cell values in the range
    /// </summary>
    /// <param name="range"></param>
    public void ClearCells(IEnumerable<IRange> ranges)
    {
        var cells = this.GetCellsInRanges(ranges);
        foreach (var cell in cells)
        {
            cell.Clear();
        }
    }

    public string GetRangeAsDelimitedText(IRange inputRange, char tabDelimiter = '\t')
    {
        if (inputRange == null)
            return string.Empty;

        var range = inputRange
                    .GetIntersection(this.Range)
                    .CopyOrdered();

        var strBuilder = new StringBuilder();

        var r0 = range.Start.Row;
        var r1 = range.End.Row;
        var c0 = range.Start.Col;
        var c1 = range.End.Col;

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

            if (row != r1)
                strBuilder.AppendLine();
        }

        return strBuilder.ToString();
    }

    /// <summary>
    /// Updates the row values of all rows & cells starting from the index specified
    /// </summary>
    /// <param name="startRow"></param>
    private void updateRowIndices(int startRow)
    {
        // Update row numbers for rows and cells below this row
        for (int i = startRow; i < _rows.Count; i++)
        {
            _rows[i].RowNumber = i;
            foreach (var cell in _rows[i].Cells)
            {
                cell.Row = i;
            }
        }
    }
}