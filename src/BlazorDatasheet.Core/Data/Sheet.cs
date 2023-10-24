using System.Diagnostics;
using System.Text;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Edit;
using BlazorDatasheet.Core.Events;
using BlazorDatasheet.Core.Events.Formula;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Validation;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Intervals;
using BlazorDatasheet.DataStructures.Store;
using BlazorDatasheet.Formula.Core;

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
    public ConditionalFormatManager ConditionalFormatting { get; }

    /// <summary>
    /// The sheet's active selection
    /// </summary>
    public Selection Selection { get; }

    internal IDialogService Dialog { get; private set; }

    /// <summary>
    /// Formats applied to any rows
    /// </summary>
    internal readonly NonOverlappingIntervals<CellFormat> RowFormats = new();

    /// <summary>
    /// Formats applied to any cols
    /// </summary>
    internal readonly NonOverlappingIntervals<CellFormat> ColFormats = new();

    #region EVENTS

    /// <summary>
    /// Fired when a row is inserted into the sheet
    /// </summary>
    public event EventHandler<RowInsertedEventArgs>? RowInserted;

    /// <summary>
    /// Fired when a row is removed from the sheet.
    /// </summary>
    public event EventHandler<RowRemovedEventArgs>? RowRemoved;

    /// <summary>
    /// Fired when a column is inserted into the sheet
    /// </summary>
    public event EventHandler<ColumnInsertedEventArgs>? ColumnInserted;

    /// <summary>
    /// Fired when a column is removed from the sheet.
    /// </summary>
    public event EventHandler<ColumnRemovedEventArgs>? ColumnRemoved;

    /// <summary>
    /// Fired when one or more cells are changed
    /// </summary>
    public event EventHandler<IEnumerable<ChangeEventArgs>>? CellsChanged;

    /// <summary>
    /// Fired when a column width is changed
    /// </summary>
    public event EventHandler<ColumnWidthChangedEventArgs>? ColumnWidthChanged;

    /// <summary>
    /// Fired when a row height is changed.
    /// </summary>
    public event EventHandler<RowHeightChangedEventArgs>? RowHeightChanged;

    /// <summary>
    /// Fired when a portion of the sheet is marked as dirty.
    /// </summary>
    public event EventHandler<DirtySheetEventArgs>? SheetDirty;

    /// <summary>
    /// Contains data, including width, on each column.
    /// </summary>
    public ColumnInfoStore ColumnInfo { get; private set; } = new(105);

    /// <summary>
    /// Contains data, including height, on each row.
    /// </summary>
    public RowInfoStore RowInfo { get; private set; } = new(25);

    public event EventHandler<CellsSelectedEventArgs>? CellsSelected;

    public event EventHandler<CellMetaDataChangeEventArgs>? MetaDataChanged;

    public event EventHandler<CellFormulaChangeEventArgs>? FormulaChanged;

    /// <summary>
    /// Fired when cell formats change
    /// </summary>
    public event EventHandler<FormatChangedEventArgs>? FormatsChanged;

    /// <summary>
    /// Fired when the sheet is invalidated (requires re-render).
    /// </summary>
    public event EventHandler<SheetInvalidateEventArgs>? SheetInvalidated;

    /// <summary>
    /// Fired before a cell's value is set. Allows for changing the value that is set.
    /// </summary>
    public event EventHandler<BeforeCellChangeEventArgs> BeforeSetCellValue;

    #endregion

    public Editor Editor { get; }

    /// <summary>
    /// Contains cell merge information and handles merges.
    /// </summary>
    public MergeManager Merges { get; }

    /// <summary>
    /// Manages and holds information on cell validators.
    /// </summary>
    public ValidationManager Validation { get; }

    /// <summary>
    /// True if the sheet is not firing dirty events until <see cref="EndBatchDirty"/> is called.
    /// </summary>
    private bool _IsBatchingDirty { get; set; }

    /// <summary>
    /// If the sheet is batching dirty regions, they are stored here.
    /// </summary>
    private List<IRegion> _dirtyRegions = new();

    /// <summary>
    /// If the sheet is batching dirty cells, they are stored here.
    /// </summary>
    private HashSet<(int row, int col)> _dirtyPositions = new();

    internal readonly IMatrixDataStore<Cell> CellDataStore = new SparseMatrixStore<Cell>();

    private Sheet()
    {
        Merges = new MergeManager(this);
        Validation = new ValidationManager();
        Commands = new CommandManager(this);
        Selection = new Selection(this);
        Editor = new Editor(this);
        FormulaEngine = new FormulaEngine.FormulaEngine(this);
        ConditionalFormatting = new ConditionalFormatManager(this);
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
                CellDataStore.Set(i, j, cell);
            }
        }
    }

    public Sheet(int numRows, int numCols) : this()
    {
        Validation = new ValidationManager();
        NumCols = numCols;
        NumRows = numRows;
    }


    #region COLS

    /// <summary>
    /// Inserts a column after the index specified. If the index is outside of the range of -1 to NumCols-1,
    /// A column is added either at the beginning or end of the columns.
    /// </summary>
    /// <param name="colIndex"></param>
    public void InsertColAt(int colIndex, int nCols = 1)
    {
        var indexToAdd = Math.Min(NumCols - 1, Math.Max(colIndex, 0));
        var cmd = new InsertColAtCommand(indexToAdd, nCols);
        Commands.ExecuteCommand(cmd);
    }

    internal void InsertColAtImpl(int colIndex, int nCols = 1)
    {
        CellDataStore.InsertColAt(colIndex, nCols);
        NumCols += nCols;
        ColumnInserted?.Invoke(this, new ColumnInsertedEventArgs(colIndex, nCols));
    }

    /// <summary>
    /// Removes the column at the specified index
    /// </summary>
    /// <param name="colIndex"></param>
    /// <param name="nCols">The number of oclumns to remove</param>
    /// <returns>Whether the column removal was successful</returns>
    public bool RemoveCol(int colIndex, int nCols = 1)
    {
        var cmd = new RemoveColumnCommand(colIndex, nCols);
        return Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Internal implementation that removes the column data
    /// </summary>
    /// <param name="colIndex"></param>
    /// <returns>Whether the column at index colIndex was removed</returns>
    internal bool RemoveColImpl(int colIndex, int nCols = 1)
    {
        CellDataStore.RemoveColAt(colIndex, nCols);
        NumCols -= nCols;
        ColumnRemoved?.Invoke(this, new ColumnRemovedEventArgs(colIndex, nCols));

        return true;
    }

    /// <summary>
    /// Sets the width of a column, to the width given (in px).
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="width"></param>
    public void SetColumnWidth(int colStart, int colEnd, double width)
    {
        var cmd = new SetColumnWidthCommand(colStart, colEnd, width);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the width of a column, to the width given (in px).
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="width"></param>
    public void SetColumnWidth(int column, double width)
    {
        var cmd = new SetColumnWidthCommand(column, column, width);
        Commands.ExecuteCommand(cmd);
    }

    internal void EmitColumnWidthChange(int colIndex, int colEnd, double width)
    {
        ColumnWidthChanged?.Invoke(this, new ColumnWidthChangedEventArgs(colIndex, colEnd, width));
    }

    public void SetColumnHeadings(int colStart, int colEnd, string heading)
    {
        Commands.ExecuteCommand(new SetColumnHeadingsCommand(colStart, colEnd, heading));
    }

    #endregion

    #region ROWS

    /// <summary>
    /// Inserts a row at an index specified.
    /// </summary>
    /// <param name="rowIndex">The index that the new row will be at. The new row will have the index specified.</param>
    public void InsertRowAt(int rowIndex)
    {
        var indexToAddAt = Math.Min(NumRows - 1, Math.Max(rowIndex, 0));
        var cmd = new InsertRowsAtCommand(indexToAddAt);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// The internal insert function that implements adding a row
    /// This function does not add a command that is able to be undone.
    /// </summary>
    /// <param name="rowIndex"></param>
    /// <param name="nRows">The number of rows to insert</param>
    /// <param name="height">The height of each row that is inserted. Default is the default row height</param>
    /// <returns></returns>
    internal bool InsertRowAtImpl(int rowIndex, int nRows = 1)
    {
        CellDataStore.InsertRowAt(rowIndex, nRows);
        NumRows += nRows;

        RowInserted?.Invoke(this, new RowInsertedEventArgs(rowIndex, nRows));
        return true;
    }

    public bool RemoveRow(int index, int nRows = 1)
    {
        var cmd = new RemoveRowsCommand(index, nRows);
        return Commands.ExecuteCommand(cmd);
    }

    internal bool RemoveRowAtImpl(int rowIndex, int nRows)
    {
        var row = rowIndex;
        var endIndex = rowIndex + nRows;
        CellDataStore.RemoveRowAt(rowIndex, nRows);

        NumRows -= nRows;
        RowRemoved?.Invoke(this, new RowRemovedEventArgs(rowIndex, nRows));
        row++;

        return true;
    }


    /// <summary>
    /// Sets the height of a column, to the height given (in px).
    /// </summary>
    /// <param name="rowStart"></param>
    /// <param name="height"></param>
    public void SetRowHeight(int rowStart, int rowEnd, double height)
    {
        var cmd = new SetRowHeightCommand(rowStart, rowEnd, height);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Sets the width of a column, to the width given (in px).
    /// </summary>
    /// <param name="colStart"></param>
    /// <param name="height"></param>
    public void SetRowHeight(int row, double height)
    {
        var cmd = new SetRowHeightCommand(row, row, height);
        Commands.ExecuteCommand(cmd);
    }

    internal void EmitRowHeightChange(int rowStart, int rowEnd, double height)
    {
        RowHeightChanged?.Invoke(this, new RowHeightChangedEventArgs(rowStart, rowEnd, height));
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
    /// Returns a column or row range, depending on the axis provided
    /// </summary>
    /// <param name="axis">The axis of the range (row or column)</param>
    /// <param name="start">The start row/column index</param>
    /// <param name="end">The end row/column index</param>
    /// <returns></returns>
    public BRange Range(Axis axis, int start, int end)
    {
        switch (axis)
        {
            case Axis.Col:
                return Range(new ColumnRegion(start, end));
            case Axis.Row:
                return Range(new RowRegion(start, end));
        }

        throw new Exception("Cannot return a range for axis " + axis);
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
        var cell = CellDataStore.Get(row, col);

        if (cell == null)
            return new Cell()
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
        return CellDataStore.GetNonEmptyPositions(region.TopLeft.Row,
                                                  region.BottomRight.Row,
                                                  region.TopLeft.Col,
                                                  region.BottomRight.Col);
    }

    #endregion

    #region DATA

    public bool SetCellValue(int row, int col, object value)
    {
        return SetCellValues(new List<CellValueChange>() { new CellValueChange(row, col, value) });
    }

    public bool SetCellValueImpl(int row, int col, object? value, bool raiseEvent = true)
    {
        var cell = CellDataStore.Get(row, col);
        if (cell == null)
        {
            cell = new Cell(value);
            CellDataStore.Set(row, col, cell);
            if (raiseEvent)
                CellsChanged?.Invoke(this, new List<ChangeEventArgs>() { new(row, col, null, value) });

            MarkDirty(row, col);
            return true;
        }

        // Try to set the cell's value to the new value
        var oldValue = cell.GetValue();
        var setValue = cell.TrySetValue(value);
        if (setValue && raiseEvent)
        {
            var args = new ChangeEventArgs[]
            {
                new(row, col, oldValue, value)
            };
            CellsChanged?.Invoke(this, args);
        }

        // Perform data validation
        // but we don't restrict the cell value being set here,
        // it is just marked as invalid if it is invalid
        var validationResult = Validation.Validate(value, row, col);
        cell.IsValid = validationResult.IsValid;

        if (setValue)
            MarkDirty(row, col);

        return setValue;
    }

    /// <summary>
    /// Sets cell metadata, specified by name, for the cell at position row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <returns>Whether setting the cell metadata was successful</returns>
    public bool SetCellMetaData(int row, int col, string name, object? value)
    {
        var cmd = new SetMetaDataCommand(row, col, name, value);
        return Commands.ExecuteCommand(cmd);
    }

    internal void SetMetaDataImpl(int row, int col, string name, object? value)
    {
        var cell = CellDataStore.Get(row, col);
        if (cell == null)
        {
            cell = new Cell();
            CellDataStore.Set(row, col, cell);
        }

        var oldMetaData = cell.GetMetaData(name);

        cell.SetCellMetaData(name, value);
        this.MetaDataChanged?.Invoke(this, new CellMetaDataChangeEventArgs(row, col, name, oldMetaData, value));
    }

    /// <summary>
    /// Returns the metadata with key "name" for the cell at row, col.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public object? GetMetaData(int row, int col, string name)
    {
        return GetCell(row, col)?.GetMetaData(name);
    }


    public void SetCell(int row, int col, Cell cell)
    {
        cell.Row = row;
        cell.Col = col;
        CellDataStore.Set(row, col, cell);
        this.MarkDirty(row, col);
    }

    public void SetCells(IEnumerable<(int row, int col, Cell cell)> cells)
    {
        this.BatchDirty();
        foreach (var cell in cells)
            SetCell(cell.row, cell.col, cell.cell);
        this.EndBatchDirty();
    }

    /// <summary>
    /// Gets the cell's value at row, col
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public object? GetValue(int row, int col)
    {
        return GetCell(row, col).GetValue();
    }

    /// <summary>
    /// Sets cell values to those specified.
    /// </summary>
    /// <param name="changes"></param>
    /// <returns></returns>
    public bool SetCellValues(List<CellValueChange> changes)
    {
        var beforeChangesEvent = new BeforeCellChangeEventArgs(changes);
        BeforeSetCellValue?.Invoke(this, beforeChangesEvent);

        if (beforeChangesEvent.Cancel)
            return false;

        var cmd = new SetCellValuesCommand(changes);
        return Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Performs the actual setting of cell values, including raising events for any changes made.
    /// </summary>
    /// <param name="changes"></param>
    /// <returns></returns>
    internal bool SetCellValuesImpl(List<CellValueChange> changes)
    {
        var changeEvents = new List<ChangeEventArgs>();
        BatchDirty();
        foreach (var change in changes)
        {
            var currValue = GetValue(change.Row, change.Col);
            var set = SetCellValueImpl(change.Row, change.Col, change.NewValue, false);
            var newValue = GetValue(change.Row, change.Col);
            if (set && currValue != newValue)
            {
                changeEvents.Add(new ChangeEventArgs(change.Row, change.Col, currValue, newValue));
            }
        }

        EndBatchDirty();
        CellsChanged?.Invoke(this, changeEvents);
        return changes.Any();
    }

    /// <summary>
    /// Set the formula string for a row and col, and calculate the sheet.
    /// If the parsed formula is invalid, the formula will not be set.
    /// </summary>
    /// <param name="formulaString"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void SetFormula(string formulaString, int row, int col)
    {
        var parsed = FormulaEngine.ParseFormula(formulaString);
        if (parsed.IsValid())
            SetFormula(parsed, row, col);
    }

    /// <summary>
    /// Sets the formula for a row and col, and calculate the sheet.
    /// </summary>
    /// <param name="parsedFormula"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public void SetFormula(CellFormula parsedFormula, int row, int col)
    {
        Commands.ExecuteCommand(new SetParsedFormulaCommand(row, col, parsedFormula, true));
    }

    internal void SetFormulaImpl(CellFormula parsedFormula, int row, int col)
    {
        var cell = CellDataStore.Get(row, col);
        if (cell == null)
        {
            cell = new Cell();
            CellDataStore.Set(row, col, cell);
        }

        var oldFormula = cell.Formula;

        cell.Formula = parsedFormula;
        this.FormulaChanged?.Invoke(this, new CellFormulaChangeEventArgs(row, col, oldFormula, parsedFormula));
    }

    /// <summary>
    /// Clears all cell values in the region
    /// </summary>
    /// <param name="range">The range in which to clear all cells</param>
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
        this.BatchDirty();
        foreach (var posn in positions)
        {
            var cell = this.GetCell(posn.row, posn.col) as Cell;
            var oldValue = cell!.GetValue();
            cell!.Clear();
            var newVal = cell.GetValue();
            if (oldValue != newVal)
            {
                changeArgs.Add(new ChangeEventArgs(posn.row, posn.col, oldValue, newVal));
                MarkDirty(posn.row, posn.col);
            }
        }

        this.EndBatchDirty();

        this.CellsChanged?.Invoke(this, changeArgs);
    }

    #endregion

    internal void ValidateRegion(IRegion region)
    {
        var cellsAffected = CellDataStore.GetNonEmptyPositions(region).ToList();
        foreach (var (row, col) in cellsAffected)
        {
            var cell = CellDataStore.Get(row, col);
            var result = Validation.Validate(cell.GetValue(), row, col);
            cell.IsValid = result.IsValid;
        }

        MarkDirty(cellsAffected);
    }

    /// <summary>
    /// Mark the cells specified by positions dirty.
    /// </summary>
    /// <param name="positions"></param>
    internal void MarkDirty(IEnumerable<(int row, int col)> positions)
    {
        if (_IsBatchingDirty)
        {
            foreach (var position in positions)
                _dirtyPositions.Add(position);
        }
        else
            SheetDirty?.Invoke(this, new DirtySheetEventArgs()
            {
                DirtyPositions = positions.ToHashSet()
            });
    }

    /// <summary>
    /// Marks the cell as dirty and requiring re-render
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    internal void MarkDirty(int row, int col)
    {
        if (_IsBatchingDirty)
            _dirtyPositions.Add((row, col));
        else
            SheetDirty?.Invoke(this, new DirtySheetEventArgs()
            {
                DirtyPositions = new HashSet<(int row, int col)>() { (row, col) }
            });
    }

    /// <summary>
    /// Marks the region as dirty and requiring re-render.
    /// </summary>
    /// <param name="region"></param>
    internal void MarkDirty(IRegion region)
    {
        MarkDirty(new List<IRegion>() { region });
    }

    /// <summary>
    /// Marks the regions as dirty and requiring re-render.
    /// </summary>
    /// <param name="regions"></param>
    internal void MarkDirty(IEnumerable<IRegion> regions)
    {
        if (_IsBatchingDirty)
            _dirtyRegions.AddRange(regions);
        else
            SheetDirty?.Invoke(
                this, new DirtySheetEventArgs() { DirtyRegions = regions, DirtyPositions = _dirtyPositions });
    }

    /// <summary>
    /// Batches dirty cell and region additions to emit a dirty sheet event once rather
    /// than every time mark dirty is called.
    /// </summary>
    internal void BatchDirty()
    {
        if (!_IsBatchingDirty)
        {
            _dirtyPositions.Clear();
            _dirtyRegions.Clear();
        }

        _IsBatchingDirty = true;
    }

    /// <summary>
    /// Ends the batching of dirty cells and regions, and emits the dirty sheet event.
    /// </summary>
    internal void EndBatchDirty()
    {
        if (_dirtyRegions.Any() || _dirtyPositions.Any())
        {
            SheetDirty?.Invoke(this, new DirtySheetEventArgs()
            {
                DirtyRegions = _dirtyRegions,
                DirtyPositions = _dirtyPositions
            });
        }
        
        _IsBatchingDirty = false;
    }

    /// <summary>
    /// Add a <see cref="IDataValidator"> to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validator"></param>
    public void AddValidator(IRegion region, IDataValidator validator)
    {
        var cmd = new SetValidatorCommand(region, validator);
        Commands.ExecuteCommand(cmd);
    }

    /// <summary>
    /// Adds multiple validators to a region.
    /// </summary>
    /// <param name="region"></param>
    /// <param name="validators"></param>
    public void AddValidators(IRegion region, IEnumerable<IDataValidator> validators)
    {
        Commands.BeginCommandGroup();
        foreach (var validator in validators)
        {
            AddValidator(region, validator);
        }

        Commands.EndCommandGroup();
    }

    /// <summary>
    /// Inserts delimited text from the given position
    /// And assigns cell's values based on the delimited text (tabs and newlines)
    /// Returns the region of cells that surrounds all cells that are affected
    /// </summary>
    /// <param name="text">The text to insert</param>
    /// <param name="inputPosition">The position where the insertion starts</param>
    /// <param name="newLineDelim">The delimiter that specifies a new-line</param>
    /// <returns>The region of cells that were affected</returns>
    public Region? InsertDelimitedText(string text, CellPosition inputPosition, string newLineDelim = "\n")
    {
        if (inputPosition.IsInvalid)
            return null;

        if (text.EndsWith('\n'))
            text = text.Substring(0, text.Length - 1);
        var lines = text.Split(newLineDelim);

        // We may reach the end of the sheet, so we only need to paste the rows up until the end.
        var endRow = Math.Min(inputPosition.Row + lines.Length - 1, NumRows - 1);
        // Keep track of the maximum end column that we are inserting into
        // This is used to determine the region to return.
        // It is possible that each line is of different cell lengths, so we return the max for all lines
        var maxEndCol = -1;

        var valChanges = new List<CellValueChange>();

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
                valChanges.Add(new CellValueChange(row, col, lineSplit[cellIndex]));
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
    public CellFormat? GetFormat(int row, int col)
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
    /// Returns the format that is visible at the cell position row, col.
    /// The order to determine which format is visible is
    /// 1. Cell format (if it exists)
    /// 2. Column format
    /// 3. Row format
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public CellFormat? GetFormat(IReadOnlyCell cell)
    {
        var rowFormat = RowFormats.Get(cell.Row);
        var colFormat = ColFormats.Get(cell.Col);
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
    /// <param name="cellFormat"></param>
    /// <param name="range"></param>
    public void SetFormat(CellFormat cellFormat, BRange range)
    {
        var cmd = new SetRangeFormatCommand(cellFormat, range);
        Commands.ExecuteCommand(cmd);
    }


    /// <summary>
    /// Performs the setting of formats to the range given, returning the individual cells that were affected.
    /// </summary>
    /// <param name="cellFormat"></param>
    /// <param name="range"></param>
    internal IEnumerable<CellChangedFormat> SetFormatImpl(CellFormat cellFormat, BRange range)
    {
        var changes = new List<CellChangedFormat>();
        foreach (var region in range.Regions)
        {
            changes.AddRange(SetFormatImpl(cellFormat, region));
        }

        return changes;
    }

    /// <summary>
    /// Performs the setting of formats to the region and returns the individual cells that were affected.
    /// </summary>
    /// <param name="cellFormat"></param>
    /// <param name="region"></param>
    /// <returns></returns>
    private List<CellChangedFormat> SetFormatImpl(CellFormat cellFormat, IRegion region)
    {
        // Keep track of all changes to individual cells
        var changes = new List<CellChangedFormat>();
        var colRegions = new List<ColumnRegion>();
        var rowRegions = new List<RowRegion>();

        if (region is ColumnRegion columnRegion)
        {
            changes.AddRange(SetColumnFormatImpl(cellFormat, columnRegion));
            colRegions.Add(columnRegion);
        }


        else if (region is RowRegion rowRegion)
        {
            changes.AddRange(SetRowFormatImpl(cellFormat, rowRegion));
            rowRegions.Add(rowRegion);
        }
        else
        {
            var sheetRegion = region.GetIntersection(this.Region);
            if (sheetRegion != null)
            {
                var positions = new BRange(this, sheetRegion).Positions;
                foreach (var cellPosition in positions)
                {
                    if (!CellDataStore.Contains(cellPosition.row, cellPosition.col))
                        CellDataStore.Set(cellPosition.row, cellPosition.col, new Cell());
                    var cell = CellDataStore.Get(cellPosition.row, cellPosition.col);
                    var oldFormat = cell!.Formatting?.Clone();
                    cell!.MergeFormat(cellFormat);
                    changes.Add(new CellChangedFormat(cellPosition.row, cellPosition.col, oldFormat, cellFormat));
                }
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

    private IEnumerable<CellChangedFormat> SetColumnFormatImpl(CellFormat cellFormat, ColumnRegion region)
    {
        // Keep track of individual cell changes
        var changes = new List<CellChangedFormat>();

        ColFormats.Add(new OrderedInterval<CellFormat>(region.Start.Col, region.End.Col, cellFormat));
        // Set the specific format of any non-empty cells in the column range (empty cells are covered by the range format).
        // We do this because cell formatting takes precedence in rendering over col & range formats.
        // So if the cell already has a format, it should be merged.
        var nonEmpty = this.GetNonEmptyCellPositions(region);
        foreach (var posn in nonEmpty)
        {
            var cell = CellDataStore.Get(posn.row, posn.col);
            var oldFormat = cell!.Formatting?.Clone();
            if (cell.Formatting == null)
                cell.Formatting = cellFormat.Clone();
            else
                cell.Formatting.Merge(cellFormat);

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
            if (sheetRegion != null)
            {
                var positions = new BRange(this, sheetRegion).Positions;
                foreach (var position in positions)
                {
                    if (!CellDataStore.Contains(position.row, position.col))
                        CellDataStore.Set(position.row, position.col, new Cell());
                    var cell = CellDataStore.Get(position.row, position.col);
                    var oldFormat = cell!.Formatting?.Clone();
                    CellDataStore.Get(position.row, position.col)!.MergeFormat(cellFormat);
                    changes.Add(new CellChangedFormat(position.row, position.col, oldFormat, cellFormat));
                }
            }
        }

        return changes;
    }

    private IEnumerable<CellChangedFormat> SetRowFormatImpl(CellFormat cellFormat, RowRegion region)
    {
        // Keep track of individual cell changes
        var changes = new List<CellChangedFormat>();

        RowFormats.Add(new OrderedInterval<CellFormat>(region.Start.Row, region.End.Row, cellFormat));
        // Set the specific format of any non-empty cells in the column range (empty cells are covered by the range format).
        // We do this because cell formatting takes precedence in rendering over col & range formats.
        // So if the cell already has a format, it should be merged.
        var nonEmpty = this.GetNonEmptyCellPositions(region);
        foreach (var posn in nonEmpty)
        {
            var cell = CellDataStore.Get(posn.row, posn.col);
            var oldFormat = cell!.Formatting?.Clone();
            if (cell.Formatting == null)
                cell.Formatting = cellFormat.Clone();
            else
                cell.Formatting.Merge(cellFormat);

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
            if (sheetRegion != null)
            {
                var posns = new BRange(this, sheetRegion).Positions;
                foreach (var posn in posns)
                {
                    if (!CellDataStore.Contains(posn.row, posn.col))
                        CellDataStore.Set(posn.row, posn.col, new Cell());
                    var cell = CellDataStore.Get(posn.row, posn.col);
                    var oldFormat = cell!.Formatting?.Clone();
                    CellDataStore.Get(posn.row, posn.col)!.MergeFormat(cellFormat);
                    changes.Add(new CellChangedFormat(posn.row, posn.col, oldFormat, cellFormat));
                }
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
    /// <param name="cellFormat"></param>
    /// <returns>A record of what change occured.</returns>
    internal CellChangedFormat SetCellFormat(int row, int col, CellFormat cellFormat)
    {
        CellFormat? oldFormat = null;
        if (!CellDataStore.Contains(row, col))
            CellDataStore.Set(row, col, new Cell() { Formatting = cellFormat });
        else
        {
            var cell = CellDataStore.Get(row, col);
            oldFormat = cell!.Formatting;
            cell!.Formatting = cellFormat;
        }

        return new CellChangedFormat(row, col, oldFormat, cellFormat);
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

    public void SetDialogService(IDialogService service)
    {
        Dialog = service;
    }
}