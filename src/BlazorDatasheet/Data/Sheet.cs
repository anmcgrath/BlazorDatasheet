using System.Text;
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

    public Range? Range => new Range(0, NumRows - 1, 0, NumCols - 1);

    public ConditionalFormatManager ConditionalFormatting { get; }
    public Selection Selection { get; }

    private readonly Dictionary<string, Type> _editorTypes;
    public IReadOnlyDictionary<string, Type> EditorTypes => _editorTypes;
    private Dictionary<string, Type> _renderComponentTypes { get; set; }
    public IReadOnlyDictionary<string, Type> RenderComponentTypes => _renderComponentTypes;

    private Sheet()
    {
        ColumnHeadings = new List<Heading>();
        RowHeadings = new List<Heading>();
        Selection = new Selection(this);
        ConditionalFormatting = new ConditionalFormatManager(this);
        _editorTypes = new Dictionary<string, Type>();
        _renderComponentTypes = new Dictionary<string, Type>();

        registerDefaultEditors();
        registerDefaultRenderers();
    }

    [Obsolete]
    public Sheet(int numRows, int numCols, Cell[,] cells) : this()
    {
        NumCols = numCols;

        for (var i = 0; i < numRows; i++)
        {
            var rowCells = new List<Cell>();
            for (int j = 0; j < NumCols; j++)
            {
                rowCells.Add(cells[i, j]);
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
                cells.Add(new Cell());
            }

            var row = new Row(cells, i);
            _rows.Add(row);
        }
    }

    public void AddRow()
    {
    }

    /// <summary>
    /// Adds a row at an index specified.
    /// </summary>
    /// <param name="rowIndex">The index that the new row will be at. If the index is outside of the range of rows,
    /// a row will be either inserted at the start or appended at the end</param>
    public void AddRowAt(int rowIndex)
    {
        int addIndex;
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

    public IEnumerable<Cell> GetCellsInRanges(IEnumerable<IRange> ranges)
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
        var cell = this.GetCell(row, col);
        if (cell == null)
            return false;

        return TrySetCellValue(cell, value);
    }

    internal bool TrySetCellValue(Cell cell, object value)
    {
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
        return cell.TrySetValue(value);
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
}