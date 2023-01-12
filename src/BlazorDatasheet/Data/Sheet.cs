using System.Text;
using BlazorDatasheet.Commands;
using BlazorDatasheet.Data.Events;
using BlazorDatasheet.Data.SpatialDataStructures;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Formats;
using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Render;
using BlazorDatasheet.Render.DefaultComponents;
using BlazorDatasheet.Selecting;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Data;

public class Sheet
{
    /// <summary>
    /// The total number of rows in the sheet
    /// </summary>
    public int NumRows { get; private set; }

    /// <summary>
    /// The total of columns in the sheet
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
    /// Formats applied to any rows
    /// </summary>
    internal NonOverlappingIntervals<Format> RowFormats = new();

    /// <summary>
    /// Formats applied to any cols
    /// </summary>
    internal NonOverlappingIntervals<Format> ColFormats = new();

    /// <summary>
    /// The merged cells in the sheet.
    /// </summary>
    internal RTree<CellMerge> MergedCells { get; } = new();

    #region EVENTS

    /// <summary>
    /// Fired when a row is inserted into the sheet
    /// </summary>
    public event EventHandler<RowInsertedEventArgs> RowInserted;

    /// <summary>
    /// Fired when a row is removed from the sheet.
    /// </summary>
    public event EventHandler<RowRemovedEventArgs> RowRemoved;

    /// <summary>
    /// Fired when a column is inserted into the sheet
    /// </summary>
    public event EventHandler<ColumnInsertedEventArgs> ColumnInserted;

    /// <summary>
    /// Fired when a column is removed from the sheet.
    /// </summary>
    public event EventHandler<ColumnRemovedEventArgs> ColumnRemoved;

    /// <summary>
    /// Fired when one or more cells are changed
    /// </summary>
    public event EventHandler<IEnumerable<ChangeEventArgs>> CellsChanged;

    /// <summary>
    /// Fired when a column width is changed
    /// </summary>
    public event EventHandler<ColumnWidthChangedArgs> ColumnWidthChanged;

    /// <summary>
    /// Fired when cells are merged
    /// </summary>
    public event EventHandler<IRegion> RegionMerged;

    /// <summary>
    /// Fired when cell formats change
    /// </summary>
    public event EventHandler<FormatChangedEventArgs> FormatsChanged;

    #endregion

    internal CellLayoutProvider LayoutProvider { get; }

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

        LayoutProvider = new CellLayoutProvider(this, 105, 25);
    }

    public Sheet(int numRows, int numCols) : this()
    {
        NumCols = numCols;
        NumRows = numRows;
        LayoutProvider = new CellLayoutProvider(this, 105, 25);
    }

    #region COLS

    /// <summary>
    /// Inserts a column after the index specified. If the index is outside of the range of -1 to NumCols-1,
    /// A column is added either at the beginning or end of the columns.
    /// </summary>
    /// <param name="colIndex"></param>
    public void InsertColAfter(int colIndex)
    {
        var indexToAddAfter = Math.Min(NumCols - 1, Math.Max(colIndex, -1));
        var cmd = new InsertColAfterCommand(indexToAddAfter);
        Commands.ExecuteCommand(cmd);
    }

    internal void InsertColAfterImpl(int colIndex)
    {
        _cellDataStore.InsertColAfter(colIndex);
        if (ColumnHeadings.Count > colIndex)
            ColumnHeadings.Insert(colIndex + 1, new Heading());
        NumCols++;
        ColumnInserted?.Invoke(this, new ColumnInsertedEventArgs(colIndex));
    }

    /// <summary>
    /// Removes the column at the specified index
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns>Whether the column removal was successful</returns>
    public bool RemoveCol(int colIndex)
    {
        var cmd = new RemoveColumnCommand(colIndex);
        return Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Internal implementation that removes the column
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns>Whether the column at index colIndex was removed</returns>
    internal bool RemoveColImpl(int colIndex)
    {
        if (colIndex >= 0 && colIndex <= NumCols - 1)
        {
            _cellDataStore.RemoveColAt(colIndex);
            if (colIndex < ColumnHeadings.Count)
                ColumnHeadings.RemoveAt(colIndex);
            NumCols--;
            ColumnRemoved?.Invoke(this, new ColumnRemovedEventArgs(colIndex));
            return true;
        }

        return false;
    }

    #endregion

    #region ROWS

    /// <summary>
    /// Inserts a row at an index specified.
    /// </summary>
    /// <param name="rowIndex">The index that the new row will be at. If the index is outside of the region of rows,
    /// a row will be either inserted at the start or appended at the end</param>
    public void InsertRowAfter(int rowIndex)
    {
        var indexToAddAfter = Math.Min(NumRows - 1, Math.Max(rowIndex, -1));
        var cmd = new InsertRowAfterCommand(indexToAddAfter);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// The internal insert function that implements adding a row
    /// This function does not add a command that is able to be undone.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="row"></param>
    /// <returns></returns>
    internal bool InsertRowAfterImpl(int rowIndex)
    {
        _cellDataStore.InsertRowAfter(rowIndex);
        NumRows++;
        RowInserted?.Invoke(this, new RowInsertedEventArgs(rowIndex));
        return true;
    }

    public bool RemoveRow(int index)
    {
        var cmd = new RemoveRowCommand(index);
        return Commands.ExecuteCommand(cmd);
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

    #endregion

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


    #region CELLS
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
    
    #endregion
    
    #region DATA
    
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
    
    #endregion

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

        if (text.EndsWith('\n'))
            text = text.Substring(0, text.Length - 1);
        var lines = text.Split(newLineDelim, StringSplitOptions.None);

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

    #region FORMAT

    /// <summary>
    /// Returns the format that is visible at the cell position row, col.
    /// The order to determine which format is visible is
    /// 1. Cell format (if it exists)
    /// 2. Column format
    /// 3. Row format
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public Format? GetFormat(int row, int col)
    {
        var cell = GetCell(row, col);
        var rowFormat = RowFormats.Get(row);
        var colFormat = ColFormats.Get(col);
        if (cell.Formatting != null)
            return cell.Formatting;
        if (colFormat != null)
            return colFormat;
        else
            return rowFormat;
    }

    /// <summary>
    /// Sets the format for a particular range
    /// </summary>
    /// <param name="format"></param>
    /// <param name="range"></param>
    public void SetFormat(Format format, BRange range)
    {
        var cmd = new SetRangeFormatCommand(format, range);
        Commands.ExecuteCommand(cmd);
    }


    /// <summary>
    /// Performs the setting of formats to the range given, returning the individual cells that were affected.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="range"></param>
    internal IEnumerable<CellChangedFormat> SetFormatImpl(Format format, BRange range)
    {
        var changes = new List<CellChangedFormat>();
        foreach (var region in range.Regions)
        {
            changes.AddRange(SetFormatImpl(format, region));
        }

        return changes;
    }

    /// <summary>
    /// Performs the setting of formats to the region and returns the individual cells that were affected.
    /// </summary>
    /// <param name="format"></param>
    /// <param name="region"></param>
    /// <returns></returns>
    internal List<CellChangedFormat> SetFormatImpl(Format format, IRegion region)
    {
        // Keep track of all changes to individual cells
        var changes = new List<CellChangedFormat>();
        var colRegions = new List<ColumnRegion>();
        var rowRegions = new List<RowRegion>();

        if (region is ColumnRegion columnRegion)
        {
            changes.AddRange(SetColumnFormatImpl(format, columnRegion));
            colRegions.Add(columnRegion);
        }


        else if (region is RowRegion rowRegion)
        {
            changes.AddRange(SetRowFormatImpl(format, rowRegion));
            rowRegions.Add(rowRegion);
        }
        else
        {
            var sheetRegion = region.GetIntersection(this.Region);
            var posns = new BRange(this, sheetRegion).Positions;
            foreach (var posn in posns)
            {
                if (!_cellDataStore.Contains(posn.row, posn.col))
                    _cellDataStore.Set(posn.row, posn.col, new Cell());
                var cell = _cellDataStore.Get(posn.row, posn.col);
                var oldFormat = cell!.Formatting?.Clone();
                cell!.MergeFormat(format);
                changes.Add(new CellChangedFormat(posn.row, posn.col, oldFormat, format));
            }
        }

        var args = new FormatChangedEventArgs(changes, colRegions, rowRegions);
        EmitFormatChanged(args);

        return changes;
    }

    internal void EmitFormatChanged(FormatChangedEventArgs args)
    {
        FormatsChanged?.Invoke(this, args);
    }

    private IEnumerable<CellChangedFormat> SetColumnFormatImpl(Format format, ColumnRegion region)
    {
        // Keep track of individual cell changes
        var changes = new List<CellChangedFormat>();

        ColFormats.Add(new OrderedInterval<Format>(region.Start.Col, region.End.Col, format));
        // Set the specific format of any non-empty cells in the column range (empty cells are covered by the range format).
        // We do this because cell formatting takes precedence in rendering over col & range formats.
        // So if the cell already has a format, it should be merged.
        var nonEmpty = this.GetNonEmptyCellPositions(region);
        foreach (var posn in nonEmpty)
        {
            var cell = _cellDataStore.Get(posn.row, posn.col);
            var oldFormat = cell!.Formatting?.Clone();
            if (cell.Formatting == null)
                cell.Formatting = format.Clone();
            else
                cell.Formatting.Merge(format);

            changes.Add(new CellChangedFormat(posn.row, posn.col, oldFormat, cell.Formatting));
        }

        // Look at the region(s) of overlap with row formats - must make these cells exist and assign formats
        var overlappingRegions = new List<IRegion>();
        foreach (var rowInterval in RowFormats.GetAllIntervals())
        {
            overlappingRegions.Add(new Region(rowInterval.Start, rowInterval.End, region.Start.Col,
                region.End.Col));
        }

        foreach (var overlapRegion in overlappingRegions)
        {
            var sheetRegion = overlapRegion.GetIntersection(this.Region);
            var posns = new BRange(this, sheetRegion).Positions;
            foreach (var posn in posns)
            {
                if (!_cellDataStore.Contains(posn.row, posn.col))
                    _cellDataStore.Set(posn.row, posn.col, new Cell());
                var cell = _cellDataStore.Get(posn.row, posn.col);
                var oldFormat = cell!.Formatting?.Clone();
                _cellDataStore.Get(posn.row, posn.col)!.MergeFormat(format);
                changes.Add(new CellChangedFormat(posn.row, posn.col, oldFormat, format));
            }
        }

        return changes;
    }

    private IEnumerable<CellChangedFormat> SetRowFormatImpl(Format format, RowRegion region)
    {
        // Keep track of individual cell changes
        var changes = new List<CellChangedFormat>();

        RowFormats.Add(new OrderedInterval<Format>(region.Start.Row, region.End.Row, format));
        // Set the specific format of any non-empty cells in the column range (empty cells are covered by the range format).
        // We do this because cell formatting takes precedence in rendering over col & range formats.
        // So if the cell already has a format, it should be merged.
        var nonEmpty = this.GetNonEmptyCellPositions(region);
        foreach (var posn in nonEmpty)
        {
            var cell = _cellDataStore.Get(posn.row, posn.col);
            var oldFormat = cell!.Formatting?.Clone();
            if (cell.Formatting == null)
                cell.Formatting = format.Clone();
            else
                cell.Formatting.Merge(format);

            changes.Add(new CellChangedFormat(posn.row, posn.col, oldFormat, cell.Formatting));
        }

        // Look at the region(s) of overlap with col formats - must make these cells exist and assign formats
        var overlappingRegions = new List<IRegion>();
        foreach (var colInterval in ColFormats.GetAllIntervals())
        {
            overlappingRegions.Add(new Region(region.Start.Row, region.End.Row, colInterval.Start,
                colInterval.End));
        }

        foreach (var overlapRegion in overlappingRegions)
        {
            var sheetRegion = overlapRegion.GetIntersection(this.Region);
            var posns = new BRange(this, sheetRegion).Positions;
            foreach (var posn in posns)
            {
                if (!_cellDataStore.Contains(posn.row, posn.col))
                    _cellDataStore.Set(posn.row, posn.col, new Cell());
                var cell = _cellDataStore.Get(posn.row, posn.col);
                var oldFormat = cell!.Formatting?.Clone();
                _cellDataStore.Get(posn.row, posn.col)!.MergeFormat(format);
                changes.Add(new CellChangedFormat(posn.row, posn.col, oldFormat, format));
            }
        }

        return changes;
    }


    /// <summary>
    /// Sets the cell format to the format specified. Note the format is set to the format
    /// and is not merged. If the cell is not in our data store it is created.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="format"></param>
    /// <returns>A record of what change occured.</returns>
    internal CellChangedFormat SetCellFormat(int row, int col, Format format)
    {
        Format? oldFormat = null;
        if (!_cellDataStore.Contains(row, col))
            _cellDataStore.Set(row, col, new Cell() { Formatting = format });
        else
        {
            var cell = _cellDataStore.Get(row, col);
            oldFormat = cell!.Formatting;
            cell!.Formatting = format;
        }

        return new CellChangedFormat(row, col, oldFormat, format);
    }
    
    #endregion

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

    internal void ClearCellsImpl(BRange range)
    {
        ClearCellsImpl(range.GetNonEmptyPositions());
    }

    internal void ClearCellsImpl(IEnumerable<(int row, int col)> positions)
    {
        var changeArgs = new List<ChangeEventArgs>();
        foreach (var posn in positions)
        {
            var cell = this.GetCell(posn.row, posn.col) as Cell;
            var oldValue = cell.GetValue();
            cell.ClearValue();
            var newVal = cell.GetValue();
            if (oldValue != newVal)
                changeArgs.Add(new ChangeEventArgs(posn.row, posn.col, oldValue, newVal));
        }

        this.CellsChanged?.Invoke(this, changeArgs);
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

    public void SetColumnWidth(int col, double width)
    {
        var cmd = new SetColumnWidthCommand(col, width);
        Commands.ExecuteCommand(cmd);
    }

    internal void SetColumnWidthImpl(int col, double width)
    {
        var oldWidth = this.LayoutProvider.ComputeWidth(col, 1);
        this.LayoutProvider.SetColumnWidth(col, width);
        ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedArgs(col, width, oldWidth));
    }

    internal bool MergeCellsImpl(BRange range)
    {
        var isSuccess = true;
        foreach (var region in range.Regions)
        {
            isSuccess &= MergeCellsImpl(region);
        }

        return isSuccess;
    }

    public void MergeCells(BRange range)
    {
        var merge = new MergeCellsCommand(range);
        Commands.ExecuteCommand(merge);
    }

    public void MergeCells(IRegion region) => MergeCells(new BRange(this, region));

    internal bool MergeCellsImpl(IRegion region)
    {
        var cellMerge = new CellMerge(region);
        MergedCells.Insert(cellMerge);
        return true;
    }

    /// <summary>
    /// Un-merge all cells that overlap the range
    /// </summary>
    /// <param name="region"></param>
    internal void UnMergeCellsImpl(IRegion region)
    {
        var envelope = region.ToEnvelope();

        var mergedCellsInRange = MergedCells.Search(envelope);
        foreach (var merge in mergedCellsInRange)
        {
            MergedCells.Delete(merge);
        }
    }

    /// <summary>
    /// Un-merge all cells that overlap the range
    /// </summary>
    /// <param name="region"></param>
    internal void UnMergeCellsImpl(BRange range)
    {
        foreach (var region in range.Regions)
            UnMergeCellsImpl(region);
    }

    /// <summary>
    /// Returns whether the position is inside a merged cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsPositionMerged(int row, int col)
    {
        return GetMergedRegionAtPosition(row, col) != null;
    }

    public IRegion? GetMergedRegionAtPosition(int row, int col)
    {
        var cellRegion = new Region(row, col);
        var merges = MergedCells.Search(cellRegion.ToEnvelope());
        // There will only be one merge because we don't allow overlapping
        return merges.FirstOrDefault()?.Region;
    }
}