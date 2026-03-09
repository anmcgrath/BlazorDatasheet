using System.Text;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Events.Sort;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.FormulaEngine;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Data;

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
    /// The total area (Num Rows x Num Cols) of the sheet.
    /// </summary>
    public int Area => NumRows * NumCols;

    /// <summary>
    /// Start/finish edits.
    /// </summary>
    public Editor Editor { get; }

    /// <summary>
    /// Interact with cells & cell data.
    /// </summary>
    public CellStore Cells { get; }

    /// <summary>
    /// Managers commands & undo/redo. Default is true.
    /// </summary>
    public CommandManager Commands { get; }

    /// <summary>
    /// Manages sheet formula
    /// </summary>
    public FormulaEngine.FormulaEngine FormulaEngine => Workbook.GetFormulaEngine();

    /// <summary>
    /// The bounds of the sheet
    /// </summary>
    public Region Region => new Region(0, NumRows - 1, 0, NumCols - 1);

    /// <summary>
    /// Provides functions for managing the sheet's conditional formatting
    /// </summary>
    public ConditionalFormatManager ConditionalFormats { get; }

    /// <summary>
    /// Manages and holds information on cell validators.
    /// </summary>
    public ValidationManager Validators { get; }

    /// <summary>
    /// Contains data, including width, on each column.
    /// </summary>
    public ColumnInfoStore Columns { get; private set; }

    /// <summary>
    /// Contains data, including height, on each row.
    /// </summary>
    public RowInfoStore Rows { get; private set; }

    /// <summary>
    /// The sheet's active selection
    /// </summary>
    public Selection Selection { get; }

    /// <summary>
    /// The sheet name.
    /// </summary>
    public string Name { get; internal set; } = "Sheet1";

    /// <summary>
    /// The frozen rows/columns on the sheet.
    /// </summary>
    public FreezeState FreezeState { get; private set; } = new FreezeState(0, 0, 0, 0);

    /// <summary>
    /// The workbook associated with this sheet.
    /// </summary>
    public Workbook Workbook
    {
        get
        {
            if (_workbook == null)
                _workbook = new Workbook(this, _givenFormulaOptions);
            return _workbook;
        }
        internal set => _workbook = value;
    }

    private Workbook? _workbook;
    private FormulaOptions? _givenFormulaOptions;

    /// <summary>
    /// Whether the UI associated with the sheet should be updating
    /// </summary>
    public bool ScreenUpdating
    {
        get => _screenUpdating;
        set
        {
            if (_screenUpdating != value)
            {
                _screenUpdating = value;
                ScreenUpdatingChanged?.Invoke(this, new SheetScreenUpdatingEventArgs(value));
            }
        }
    }

    private bool _screenUpdating = true;

    internal IDialogService? Dialog { get; private set; }

    #region EVENTS

    /// <summary>
    /// Fired when a portion of the sheet is marked as dirty.
    /// </summary>
    public event EventHandler<DirtySheetEventArgs>? SheetDirty;

    public event EventHandler<BeforeRangeSortEventArgs>? BeforeRangeSort;

    public event EventHandler<RangeSortedEventArgs>? RangeSorted;

    public event EventHandler<SheetFrozenRowColsEventArgs>? FrozenRowCols;

    /// <summary>
    /// Fired when <see cref="ScreenUpdating"/> is changed
    /// </summary>
    public EventHandler<SheetScreenUpdatingEventArgs>? ScreenUpdatingChanged;

    /// <summary>
    /// Fired before a auto fill occurs. The value can be modified.
    /// </summary>
    public event EventHandler<BeforeAutoFillEventArgs>? BeforeAutoFill;

    internal BeforeAutoFillEventArgs EmitBeforeAutoFill()
    {
        var args = new BeforeAutoFillEventArgs();
        BeforeAutoFill?.Invoke(this, args);
        return args;
    }

    #endregion EVENTS

    /// <summary>
    /// True if the sheet is not firing dirty events until <see cref="EndBatchUpdates"/> is called.
    /// </summary>
    private bool _isBatchingChanges;

    public NamedRangeManager NamedRanges { get; }

    /// <summary>
    /// If the sheet is batching dirty rows, they are stored here.
    /// </summary>
    private readonly Range1DStore<bool> _dirtyRows = new(false);

    private Sheet(int numRows, int numCols, double defaultWidth, double defaultHeight,
        FormulaOptions? formulaOptions, CellValue[][]? values)
    {
        NumCols = numCols;
        NumRows = numRows;
        _givenFormulaOptions = formulaOptions;

        Cells = values == null ? new CellStore(this) : new CellStore(this, values);
        Commands = new CommandManager(this);
        Editor = new Editor(this);
        Validators = new ValidationManager(this);
        Rows = new RowInfoStore(defaultHeight, this);
        Columns = new ColumnInfoStore(defaultWidth, this);
        Selection = new Selection(this);
        ConditionalFormats = new ConditionalFormatManager(this, Cells);
        NamedRanges = new NamedRangeManager(this);
    }

    public Sheet(int numRows, int numCols, CellValue[][] values) : this(numRows, numCols, 105, 24, null, values)
    {
    }

    internal Sheet(int numRows, int numCols, double defaultWidth, double defaultHeight, Workbook workbook) : this(
        numRows, numCols, defaultWidth, defaultHeight, null, null)
    {
        _workbook = workbook;
    }

    public Sheet(int numRows, int numCols, int defaultWidth = 105, int defaultHeight = 24,
        FormulaOptions? formulaOptions = null) : this(numRows, numCols, (double)defaultWidth, defaultHeight,
        formulaOptions, null)
    {
    }

    public Sheet(int numRows, int numCols, double defaultWidth, double defaultHeight,
        FormulaOptions? formulaOptions = null) : this(numRows, numCols, defaultWidth, defaultHeight, formulaOptions,
        null)
    {
    }

    /// <summary>
    /// Removes <paramref name="count"/> rows or columns, depending on the axis provided.
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="count"></param>
    public void Remove(Axis axis, int count)
    {
        if (axis == Axis.Col)
            RemoveCols(count);
        else
            RemoveRows(count);
    }

    /// <summary>
    /// Adds <paramref name="count"/> rows or columns, depending on the axis provided.
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="count"></param>
    public void Add(Axis axis, int count)
    {
        if (axis == Axis.Col)
            AddCols(count);
        else
            AddRows(count);
    }

    #region COLS

    private void AddCols(int nCols = 1)
    {
        NumCols += nCols;
    }

    private void RemoveCols(int nCols = 1)
    {
        NumCols -= nCols;
        Selection.ConstrainSelectionToSheet();
    }

    #endregion COLS

    #region ROWS

    private void AddRows(int nRows = 1)
    {
        NumRows += nRows;
    }

    private void RemoveRows(int nRows)
    {
        NumRows -= nRows;
        Selection.ConstrainSelectionToSheet();
    }

    #endregion ROWS

    /// <summary>
    /// Returns a single cell range at the position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public SheetRange Range(int row, int col)
    {
        return new SheetRange(this, row, col);
    }

    /// <summary>
    /// Returns a range with the positions specified
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="rowEnd"></param>
    /// <param name="colStart"></param>
    /// <param name="colEnd"></param>
    /// <returns></returns>
    public SheetRange Range(int rowStart, int rowEnd, int colStart, int colEnd)
    {
        return Range(new Region(rowStart, rowEnd, colStart, colEnd));
    }

    /// <summary>
    /// Returns a new range that contains the region specified
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public SheetRange Range(IRegion region)
    {
        return new SheetRange(this, region);
    }

    /// <summary>
    /// Returns whether the cell at position <paramref name="row"/>, <paramref name="col"/> is hidden
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool IsCellVisible(int row, int col)
    {
        return Rows.IsVisible(row) && Columns.IsVisible(col);
    }

    /// <summary>
    /// The <see cref="SheetRange"/> specified by the string e.g A1, B1:B4, A:B, A:A, 2:4, etc.
    /// </summary>
    public SheetRange? Range(string rangeStr)
    {
        if (string.IsNullOrEmpty(rangeStr))
            return null;

        var rangeStrFormula = $"={rangeStr}";
        var evaluatedValue = FormulaEngine.Evaluate(FormulaEngine.ParseFormula(rangeStrFormula, Name, true),
            resolveReferences: false);
        if (evaluatedValue.ValueType == CellValueType.Reference)
        {
            var reference = evaluatedValue.GetValue<Reference>();
            return Range(reference!.Region);
        }

        return null;
    }

    /// <summary>
    /// Returns a column or row range, depending on the axis provided
    /// </summary>
    /// <param name="axis">The axis of the range (row or column)</param>
    /// <param name="start">The start row/column index</param>
    /// <param name="end">The end row/column index</param>
    /// <returns></returns>
    public SheetRange? Range(Axis axis, int start, int end)
    {
        switch (axis)
        {
            case Axis.Col:
                return Range(new ColumnRegion(start, end));

            case Axis.Row:
                return Range(new RowRegion(start, end));
        }

        return null;
    }

    /// <summary>
    /// Marks the cell as dirty and requiring re-render
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal void MarkDirty(int row, int col)
    {
        if (!Region.Contains(row, col))
            return;
        _dirtyRows.Set(row, row, true);
    }

    /// <summary>
    /// Marks the region as dirty and requiring re-render.
    /// </summary>
    /// <param name="region"></param>
    internal void MarkDirty(IRegion region)
    {
        if (!region.Intersects(Region))
            return;

        _dirtyRows.Set(region.Top, region.Bottom, true);
        if (!_isBatchingChanges)
            EmitSheetDirty();
    }

    /// <summary>
    /// Marks the regions as dirty and requiring re-render.
    /// </summary>
    /// <param name="regions"></param>
    internal void MarkDirty(IEnumerable<IRegion> regions)
    {
        foreach (var region in regions)
        {
            if (!region.Intersects(this.Region))
                continue;

            _dirtyRows.Set(region.Top, region.Bottom, true);
        }

        if (!_isBatchingChanges)
            EmitSheetDirty();
    }

    private void EmitSheetDirty()
    {
        SheetDirty?.Invoke(this, new()
        {
            DirtyRows = _dirtyRows
        });
        _dirtyRows.Clear();
    }

    private int _batchRequestNo;

    /// <summary>
    /// Batches dirty cell and region additions, as well as cell value changes to emit events once rather
    /// than every time a cell is dirty or a value is changed.
    /// <returns>Returns false if the sheet was already batching, true otherwise.</returns>
    /// </summary>
    public void BatchUpdates()
    {
        _batchRequestNo++;

        if (_isBatchingChanges)
            return;

        _dirtyRows.Clear();
        Cells.BatchChanges();
        _isBatchingChanges = true;
    }

    public void SortRange(IRegion? region, List<ColumnSortOptions>? sortOptions = null)
    {
        if (region == null)
            return;

        sortOptions ??= [new ColumnSortOptions(0, true)];

        var beforeArgs = new BeforeRangeSortEventArgs(region, sortOptions);
        BeforeRangeSort?.Invoke(this, beforeArgs);

        var cmd = new SortRangeCommand(region, sortOptions);

        if (!beforeArgs.Cancel)
            Commands.ExecuteCommand(cmd);

        var afterArgs = new RangeSortedEventArgs(region, cmd.SortedRegion, cmd.OldIndices);
        RangeSorted?.Invoke(this, afterArgs);
    }

    /// <summary>
    /// Ends the batching of dirty cells and regions, and emits the dirty sheet event.
    /// </summary>
    public void EndBatchUpdates()
    {
        _batchRequestNo--;

        if (_batchRequestNo > 0)
            return;

        Cells.EndBatchChanges();

        // Checks for batching changes here, because the cells changed event
        // may start batching more dirty changes again from subscribers of that event.
        if (_dirtyRows.Any() && _isBatchingChanges)
            EmitSheetDirty();

        _isBatchingChanges = false;
    }

    /// <summary>
    /// Inserts delimited text from the given position
    /// And assigns cell's values based on the delimited text (tabs and newlines)
    /// Returns the region of cells that surrounds all cells that are affected
    /// </summary>
    /// <param name="text">The text to insert</param>
    /// <param name="inputPosition">The position where the insertion starts</param>
    /// <returns>The region of cells that were affected</returns>
    public Region? InsertDelimitedText(string text, CellPosition inputPosition)
    {
        if (!Region.Contains(inputPosition))
            return null;

        ReadOnlySpan<string> lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        if (lines[^1] is [])
            lines = lines[..^1];

        // We may reach the end of the sheet, so we only need to paste the rows up until the end.
        var endRow = Math.Min(inputPosition.row + lines.Length - 1, NumRows - 1);
        // Keep track of the maximum end column that we are inserting into
        // This is used to determine the region to return.
        // It is possible that each line is of different cell lengths, so we return the max for all lines
        var maxEndCol = -1;

        object[][] rowData = new object[lines.Length][];

        int lineNo = 0;
        for (int row = inputPosition.row; row <= endRow; row++)
        {
            string[] lineSplit = lines[lineNo].Split('\t');
            rowData[lineNo] = lineSplit;

            var endCol = Math.Min(inputPosition.col + lineSplit.Length - 1, NumCols - 1);
            maxEndCol = Math.Max(endCol, maxEndCol);

            lineNo++;
        }

        var inputRegion = new Region(inputPosition.row, endRow, inputPosition.col, maxEndCol);
        if (Cells.ContainsReadOnly(inputRegion))
            return null;

        Cells.SetValues(inputPosition.row, inputPosition.col, rowData);

        return inputRegion;
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
    public CellFormat GetFormat(int row, int col)
    {
        var defaultFormat = new CellFormat();
        var cellFormat = (Cells.GetFormat(row, col) ?? new CellFormat()).Clone();
        var rowFormat = Rows.Formats.Get(row)?.Clone() ?? defaultFormat;
        var colFormat = Columns.Formats.Get(col)?.Clone() ?? defaultFormat;

        rowFormat.Merge(colFormat);
        rowFormat.Merge(cellFormat);
        return rowFormat;
    }

    /// <summary>
    /// Sets the format for a particular range
    /// </summary>
    /// <param name="region"></param>
    /// <param name="cellFormat"></param>
    public void SetFormat(IRegion region, CellFormat cellFormat)
    {
        var cmd = new SetFormatCommand(region, cellFormat);
        Commands.ExecuteCommand(cmd);
    }

    public void SetFormat(IEnumerable<IRegion> regions, CellFormat cellFormat)
    {
        Commands.BeginCommandGroup();
        foreach (var region in regions)
        {
            Commands.ExecuteCommand(new SetFormatCommand(region, cellFormat));
        }

        Commands.EndCommandGroup();
    }

    #endregion FORMAT

    /// <summary>
    /// Gets the region values as a delimited string
    /// </summary>
    /// <param name="inputRegion">The region to grab</param>
    /// <param name="tabDelimiter">Separator between columns in each row</param>
    /// <param name="newLineDelim">New line character, defaults to Environment.NewLine</param>
    /// <param name="includeColHeaders">Optional, includes the column headers in the first row, for the region</param>
    /// <param name="includeRowHeaders">Optional, includes the column headers in the first column, for each line</param>
    /// <returns></returns>
    public string? GetRegionAsDelimitedText(IRegion inputRegion, char tabDelimiter = '\t', string? newLineDelim = null,
        bool includeColHeaders = false, bool includeRowHeaders = false)
    {
        newLineDelim ??= Environment.NewLine;
        var tabDelimiterAsString = tabDelimiter.ToString();

        string SanitizeDelimitedText(string value)
        {
            return value
                .Replace("\r\n", " ")
                .Replace('\n', ' ')
                .Replace('\r', ' ')
                .Replace(tabDelimiterAsString, " ");
        }

        if (inputRegion.Area == 0)
            return string.Empty;

        var intersection = inputRegion.GetIntersection(this.Region);
        if (intersection == null)
            return null;

        var range = intersection.Copy();

        var strBuilder = new StringBuilder();

        var r0 = range.TopLeft.row;
        var r1 = range.BottomRight.row;
        var c0 = range.TopLeft.col;
        var c1 = range.BottomRight.col;

        if (includeColHeaders)
        {
            if (includeRowHeaders)
                strBuilder.Append(tabDelimiter);

            for (int col = c0; col <= c1; col++)
            {
                var heading = Columns.GetHeading(col) ?? RangeText.ColIndexToLetters(col);
                strBuilder.Append(SanitizeDelimitedText(heading));
                if (col != c1)
                    strBuilder.Append(tabDelimiter);
            }

            strBuilder.Append(newLineDelim);
        }

        for (int row = r0; row <= r1; row++)
        {
            if (includeRowHeaders)
            {
                var rowHeading = Rows.GetHeading(row) ?? (row + 1).ToString();
                strBuilder.Append(SanitizeDelimitedText(rowHeading));
                strBuilder.Append(tabDelimiter);
            }

            for (int col = c0; col <= c1; col++)
            {
                var cell = Cells.GetCell(row, col);
                var value = cell.Value;
                if (value == null)
                    strBuilder.Append("");
                else
                {
                    if (value is string s)
                    {
                        strBuilder.Append(SanitizeDelimitedText(s));
                    }
                    else
                    {
                        strBuilder.Append(value);
                    }
                }

                if (col != c1)
                    strBuilder.Append(tabDelimiter);
            }

            if (row != r1)
                strBuilder.Append(newLineDelim);
        }

        return strBuilder.ToString();
    }

    public async Task<List<CellValue>> GetDistinctColumnDataAsync(int column)
    {
        // TODO: Allow custom function for get column data
        if (column < 0 || column > NumCols - 1)
            return new List<CellValue>();

        var cells = Cells.GetNonEmptyCellValues(new ColumnRegion(column))
            .Select(x => x.value)
            .DistinctBy(x => x.Data)
            .ToList();

        if (cells.Count != this.NumRows)
        {
            // there are blanks in the column
            cells.Add(CellValue.Empty);
        }

        return cells;
    }

    /// <summary>
    /// Returns the size (number of rows or columns) across the specified axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int GetSize(Axis axis)
    {
        return axis == Axis.Col ? NumCols : NumRows;
    }

    internal RowColInfoStore GetRowColStore(Axis axis)
    {
        return axis == Axis.Row ? Rows : Columns;
    }

    public void SetDialogService(IDialogService? service)
    {
        Dialog = service;
    }

    /// <summary>
    /// Returns whether the region has any visible cells (both visible rows and visible columns).
    /// </summary>
    public bool HasVisibleCells(IRegion? region)
    {
        if (region == null)
            return false;
        return Rows.CountVisible(region.Top, region.Bottom) > 0
               && Columns.CountVisible(region.Left, region.Right) > 0;
    }

    /// <summary>
    /// Returns whether the region has any visible rows.
    /// </summary>
    public bool HasVisibleRows(IRegion? region)
    {
        if (region == null)
            return false;
        return Rows.CountVisible(region.Top, region.Bottom) > 0;
    }

    /// <summary>
    /// Returns whether the region has any visible columns.
    /// </summary>
    public bool HasVisibleCols(Region? region)
    {
        if (region == null)
            return false;
        return Columns.CountVisible(region.Left, region.Right) > 0;
    }

    /// <summary>
    /// Freeze the number of row/columns specified
    /// </summary>
    /// <param name="top">The number of rows at the top of the sheet</param>
    /// <param name="bottom">The number of rows at the bottom of the sheet</param>
    /// <param name="left">The number of columns at the left of the sheet</param>
    /// <param name="right">The number of columns at the right of the sheet</param>
    public void FreezeRowCols(int top, int bottom, int left, int right)
    {
        Commands.ExecuteCommand(new FreezeRowColsCommand(top, bottom, left, right));
    }

    internal void FreezeRowColsImpl(int top, int bottom, int left, int right)
    {
        if (top < 0 || bottom < 0 || left < 0 || right < 0 || (top + bottom) > Region.Height ||
            (left + right) > Region.Width)
            throw new ArgumentException(
                $"Invalid frozen row/cols top, bottom, left and right: {top}, {bottom}, {left}, {right}");

        var oldState = FreezeState;
        FreezeState = new FreezeState(top, bottom, left, right);
        FrozenRowCols?.Invoke(this, new SheetFrozenRowColsEventArgs(oldState, FreezeState));
    }

    /// <summary>
    /// Freezes the top <paramref name="number"/> of rows
    /// </summary>
    /// <param name="number"></param>
    public void FreezeTopRows(int number)
    {
        FreezeRowCols(number, FreezeState.Bottom, FreezeState.Left, FreezeState.Right);
    }

    /// <summary>
    /// Freezes the bottom <paramref name="number"/> of rows
    /// </summary>
    /// <param name="number"></param>
    public void FreezeBottomRows(int number)
    {
        FreezeRowCols(FreezeState.Top, number, FreezeState.Left, FreezeState.Right);
    }

    /// <summary>
    /// Freezes the left <paramref name="number"/> of columns
    /// </summary>
    /// <param name="number"></param>
    public void FreezeLeftColumns(int number)
    {
        FreezeRowCols(FreezeState.Top, FreezeState.Bottom, number, FreezeState.Right);
    }

    /// <summary>
    /// Freezes the right <paramref name="number"/> of columns
    /// </summary>
    /// <param name="number"></param>
    public void FreezeRightColumns(int number)
    {
        FreezeRowCols(FreezeState.Top, FreezeState.Bottom, FreezeState.Left, number);
    }

    /// <summary>
    /// Freeze the <paramref name="edge"/> by the <paramref name="number"/> of columns
    /// </summary>
    /// <param name="edge"></param>
    /// <param name="number"></param>
    public void Freeze(Edge edge, int number)
    {
        switch (edge)
        {
            case Edge.Bottom:
                FreezeBottomRows(number); break;
            case Edge.Left:
                FreezeLeftColumns(number); break;
            case Edge.Top:
                FreezeTopRows(number); break;
            case Edge.Right:
                FreezeRightColumns(number); break;
        }
    }
}
