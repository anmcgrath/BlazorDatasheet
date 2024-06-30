using System.Diagnostics;
using System.Text;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
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
    public FormulaEngine.FormulaEngine FormulaEngine { get; }

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

    internal IDialogService? Dialog { get; private set; }

    #region EVENTS

    /// <summary>
    /// Fired when a portion of the sheet is marked as dirty.
    /// </summary>
    public event EventHandler<DirtySheetEventArgs>? SheetDirty;

    public event EventHandler<BeforeRangeSortEventArgs>? BeforeRangeSort;

    public event EventHandler<RangeSortedEventArgs>? RangeSorted;

    #endregion

    /// <summary>
    /// True if the sheet is not firing dirty events until <see cref="EndBatchUpdates"/> is called.
    /// </summary>
    private bool _isBatchingChanges;

    /// <summary>
    /// If the sheet is batching dirty regions, they are stored here.
    /// </summary>
    private readonly ConsolidatedDataStore<bool> _dirtyRegions = new();

    private Sheet()
    {
        Cells = new Cells.CellStore(this);
        Commands = new CommandManager(this);
        Editor = new Editor(this);
        Validators = new ValidationManager(this);
        Rows = new RowInfoStore(24, this);
        Columns = new ColumnInfoStore(105, this);
        Selection = new Selection(this);
        FormulaEngine = new FormulaEngine.FormulaEngine(this);
        ConditionalFormats = new ConditionalFormatManager(this, Cells);
    }

    public Sheet(int numRows, int numCols) : this()
    {
        NumCols = numCols;
        NumRows = numRows;
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

    #endregion

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

    #endregion

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
    /// Multiple regions can be included by separating them with a ","
    /// </summary>
    public SheetRange? Range(string rangeStr)
    {
        if (string.IsNullOrEmpty(rangeStr))
            return null;

        var rangeStrFormula = $"={rangeStr}";
        var evaluatedValue =
            FormulaEngine.Evaluate(FormulaEngine.ParseFormula(rangeStrFormula), resolveReferences: false);
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
    /// Mark the cells specified by positions dirty.
    /// </summary>
    /// <param name="positions"></param>
    internal void MarkDirty(IEnumerable<CellPosition> positions)
    {
        MarkDirty(positions.Select(p => new Region(p.row, p.col)));
    }

    /// <summary>
    /// Marks the cell as dirty and requiring re-render
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal void MarkDirty(int row, int col)
    {
        MarkDirty(new Region(row, col));
    }

    /// <summary>
    /// Marks the region as dirty and requiring re-render.
    /// </summary>
    /// <param name="region"></param>
    internal void MarkDirty(IRegion region)
    {
        var intersection = region.GetIntersection(this.Region);
        if (intersection == null)
            return;

        MarkDirty(new List<IRegion>() { intersection });
    }

    /// <summary>
    /// Marks the regions as dirty and requiring re-render.
    /// </summary>
    /// <param name="regions"></param>
    internal void MarkDirty(IEnumerable<IRegion> regions)
    {
        foreach (var region in regions)
        {
            var intersection = region.GetIntersection(this.Region);
            if (intersection == null)
                continue;

            _dirtyRegions.Add(intersection, true);
        }

        if (!_isBatchingChanges)
        {
            SheetDirty?.Invoke(this, new DirtySheetEventArgs()
            {
                DirtyRegions = _dirtyRegions,
            });
            _dirtyRegions.Clear();
        }
    }

    private int _batchRequestNo = 0;

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

        _dirtyRegions.Clear();
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
        if (_dirtyRegions.Any() && _isBatchingChanges)
        {
            SheetDirty?.Invoke(this, new DirtySheetEventArgs()
            {
                DirtyRegions = _dirtyRegions,
            });
        }

        _isBatchingChanges = false;
        _dirtyRegions.Clear();
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

        var valChanges = new List<(int row, int col, object value)>();

        int lineNo = 0;
        for (int row = inputPosition.row; row <= endRow; row++)
        {
            var lineSplit = lines[lineNo].Split('\t');
            // Same thing as above with the number of columns
            var endCol = Math.Min(inputPosition.col + lineSplit.Length - 1, NumCols - 1);

            maxEndCol = Math.Max(endCol, maxEndCol);

            int cellIndex = 0;
            for (int col = inputPosition.col; col <= endCol; col++)
            {
                valChanges.Add((row, col, lineSplit[cellIndex]));
                cellIndex++;
            }

            lineNo++;
        }

        Cells.SetValues(valChanges);

        return new Region(inputPosition.row, endRow, inputPosition.col, maxEndCol);
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
        var cellFormat = Cells.GetFormat(row, col).Clone();
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

    #endregion

    public string? GetRegionAsDelimitedText(IRegion inputRegion, char tabDelimiter = '\t', string newLineDelim = "\n")
    {
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

        for (int row = r0; row <= r1; row++)
        {
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
                        strBuilder.Append(s.Replace(newLineDelim, " ").Replace(tabDelimiter, ' '));
                    }
                    else
                    {
                        strBuilder.Append(value);
                    }
                }

                if (col != c1)
                    strBuilder.Append(tabDelimiter);
            }

            strBuilder.Append(newLineDelim);
        }

        return strBuilder.ToString();
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
}