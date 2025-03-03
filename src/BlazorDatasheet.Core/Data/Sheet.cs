using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Data.Cells;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.References;
using System.Text;
using BlazorDatasheet.Core.Events.Sort;
using BlazorDatasheet.Core.FormulaEngine;

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
    /// The workbook associated with this sheet.
    /// </summary>
    public Workbook Workbook
    {
        get
        {
            if (_workbook == null)
                _workbook = new Workbook(this);
            return _workbook;
        }
        internal set => _workbook = value;
    }

    private Workbook? _workbook;

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

    /// <summary>
    /// Fired when <see cref="ScreenUpdating"/> is changed
    /// </summary>
    public EventHandler<SheetScreenUpdatingEventArgs>? ScreenUpdatingChanged;

    #endregion EVENTS

    /// <summary>
    /// True if the sheet is not firing dirty events until <see cref="EndBatchUpdates"/> is called.
    /// </summary>
    private bool _isBatchingChanges;

    public NamedRangeManager NamedRanges { get; }

    /// <summary>
    /// If the sheet is batching dirty regions, they are stored here.
    /// </summary>
    private readonly ConsolidatedDataStore<bool> _dirtyRegions = new();

    private Sheet(int defaultWidth, int defaultHeight)
    {
        Cells = new CellStore(this);
        Commands = new CommandManager(this);
        Editor = new Editor(this);
        Validators = new ValidationManager(this);
        Rows = new RowInfoStore(defaultHeight, this);
        Columns = new ColumnInfoStore(defaultWidth, this);
        Selection = new Selection(this);
        ConditionalFormats = new ConditionalFormatManager(this, Cells);
        NamedRanges = new NamedRangeManager(this);
    }

    internal Sheet(int numRows, int numCols, int defaultWidth, int defaultHeight, Workbook workbook) : this(numRows,
        numCols, defaultWidth, defaultHeight)
    {
        _workbook = workbook;
    }

    public Sheet(int numRows, int numCols, int defaultWidth = 105, int defaultHeight = 24) : this(defaultWidth,
        defaultHeight)
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

    public void ClearFormat(IRegion region)
    {
    }

    #endregion FORMAT

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
    /// Expands the <paramref name="region"/> so that it covers any merged cells
    /// </summary>
    internal IRegion? ExpandRegionOverMerges(IRegion? region)
    {
        if (region is ColumnRegion || region is RowRegion)
            return region;

        // Look at the four sides of the active region
        // If any of the sides are touching active regions, we check whether the selection
        // covers the region entirely. If not, expand the sides so that they cover.
        // Continue until there are no more merge intersections that we don't fully cover.
        var boundedRegion = region?.Copy();

        if (boundedRegion == null)
            return null;

        List<IRegion> mergeOverlaps;
        do
        {
            var top = boundedRegion.GetEdge(Edge.Top);
            var right = boundedRegion.GetEdge(Edge.Right);
            var left = boundedRegion.GetEdge(Edge.Left);
            var bottom = boundedRegion.GetEdge(Edge.Bottom);

            mergeOverlaps =
                this.Cells
                    .GetMerges(new[] { top, right, left, bottom })
                    .Where(x => !boundedRegion.Contains(x))
                    .ToList();

            // Expand bounded selection to cover all the merges
            foreach (var merge in mergeOverlaps)
            {
                boundedRegion = boundedRegion.GetBoundingRegion(merge);
            }
        } while (mergeOverlaps.Any());

        return boundedRegion;
    }

    /// <summary>
    /// Contracts the <paramref name="region"/> so that its edges no longer intersects any merged regions.
    /// Returns null if it's not possible to contract.
    /// </summary>
    internal IRegion? ContractRegionOverMerges(IRegion? region)
    {
        if (region is ColumnRegion || region is RowRegion)
            return region;

        // Look at the four sides of the active region
        // If any of the sides are touching active regions, we check whether the selection
        // covers the region entirely. If not, contract the sides
        // Continue until there are no more merge intersections that we don't fully cover.
        var boundedRegion = region?.Copy();

        if (boundedRegion == null)
            return null;

        List<IRegion> mergeOverlaps;
        do
        {
            var top = boundedRegion.GetEdge(Edge.Top);
            var right = boundedRegion.GetEdge(Edge.Right);
            var left = boundedRegion.GetEdge(Edge.Left);
            var bottom = boundedRegion.GetEdge(Edge.Bottom);

            mergeOverlaps =
                this.Cells
                    .GetMerges(new[] { top, right, left, bottom })
                    .Where(x => !x.Equals(boundedRegion) && !(boundedRegion.Contains(x) && x.Area < boundedRegion.Area))
                    .Distinct()
                    .ToList();

            // Expand bounded selection to cover all the merges
            foreach (var merge in mergeOverlaps)
            {
                var mergeSpansRight = merge.SpansCol(boundedRegion.Right);
                var mergeSpansLeft = merge.SpansCol(boundedRegion.Left);
                var mergeSpansBottom = merge.SpansRow(boundedRegion.Bottom);
                var mergeSpansTop = merge.SpansRow(boundedRegion.Top);

                // if both sides of the boundedRect are inside the merge
                // it is impossible to contract
                if (mergeSpansRight && mergeSpansLeft && boundedRegion.Width < merge.Width)
                    return null;
                if (mergeSpansBottom && mergeSpansTop && boundedRegion.Height < merge.Height)
                    return null;

                var intersection = merge.GetIntersection(boundedRegion);
                if (intersection == null)
                    continue;

                if (mergeSpansRight && boundedRegion.Right != merge.Right)
                    boundedRegion.Contract(Edge.Right, intersection.Width);
                if (mergeSpansLeft && boundedRegion.Left != merge.Left)
                    boundedRegion.Contract(Edge.Left, intersection.Width);

                if (mergeSpansBottom && boundedRegion.Bottom != merge.Bottom)
                    boundedRegion.Contract(Edge.Bottom, intersection.Height);
                if (mergeSpansTop && boundedRegion.Top != merge.Top)
                    boundedRegion.Contract(Edge.Top, intersection.Height);
            }
        } while (mergeOverlaps.Any());

        return boundedRegion;
    }
}