using System.Text;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;

namespace BlazorDatasheet.Data;

public class Sheet
{
    public int NumRows { get; private set; }
    public int NumCols { get; private set; }
    public List<Row> Rows { get; set; }
    public List<Heading> ColumnHeadings { get; private set; }
    public List<Heading> RowHeadings { get; private set; }
    private readonly Dictionary<string, ConditionalFormat> _conditionalFormats;
    internal IReadOnlyDictionary<string, ConditionalFormat> ConditionalFormats => _conditionalFormats;
    private Dictionary<string, Cell[]> _cellsInConditionalFormatCache = new Dictionary<string, Cell[]>();
    public Range Range => new Range(0, NumRows - 1, 0, NumCols - 1);
    
    private Dictionary<string, Type> _editorTypes;
    public IReadOnlyDictionary<string, Type> EditorTypes => _editorTypes;
    private Dictionary<string, Type> _renderComponentTypes { get; set; }
    public IReadOnlyDictionary<string, Type> RenderComponentTypes => _renderComponentTypes;

    public Sheet(int numRows, int numCols, Cell[,] cells)
    {
        NumRows = numRows;
        NumCols = numCols;
        _conditionalFormats = new Dictionary<string, ConditionalFormat>();
        ColumnHeadings = new List<Heading>();
        RowHeadings = new List<Heading>();
        _conditionalFormats = new Dictionary<string, ConditionalFormat>();
        _editorTypes = new Dictionary<string, Type>();
        _renderComponentTypes = new Dictionary<string, Type>();

        registerDefaultEditors();
        registerDefaultRenderers();

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

        _cellsInConditionalFormatCache[key] = GetCellsInRanges(cf.Ranges.ToList());
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
            var cellsWithConditionalFormat = _cellsInConditionalFormatCache[id];
            var apply = conditionalFormat.Rule.Invoke(cell, cellsWithConditionalFormat);
            if (apply)
                format.Merge(conditionalFormat.FormatFunc.Invoke(cell, cellsWithConditionalFormat));
        }

        return format;
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
    internal Range InsertDelimitedText(string text, CellPosition inputPosition)
    {
        if (inputPosition == null)
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

    public bool TrySetCellValue(Cell cell, object value)
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
        return cell.SetValue(value);
    }

    public string GetRangeAsDelimitedText(IReadOnlyRange inputRange, char tabDelimiter = '\t')
    {
        if (inputRange == null)
            return string.Empty;

        var range = inputRange.CopyOrdered();
        range.Constrain(this.Range);

        var strBuilder = new StringBuilder();

        for (int row = range.RowStart; row <= range.RowEnd; row++)
        {
            for (int col = range.ColStart; col <= range.ColEnd; col++)
            {
                var cell = this.GetCell(row, col);
                var value = cell.GetValue();
                if (value == null)
                    strBuilder.Append("");
                else
                    strBuilder.Append(value.ToString());
                if (col != range.ColEnd)
                    strBuilder.Append(tabDelimiter);
            }

            if (row != range.RowEnd)
                strBuilder.AppendLine();
        }

        return strBuilder.ToString();
    }
}