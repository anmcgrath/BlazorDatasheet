using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Commands.Tables;
using BlazorDatasheet.Core.Events.Commands;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter.Addresses;
using BlazorDatasheet.Formula.Core.Interpreter.References;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// The workbook's tables: regions whose columns can be referred to by name in formulas, e.g. Sales[Amount] or
/// [@Qty]. Table names are unique within the workbook, ignoring case.
/// <para>
/// A region table's first row holds its column names, which follow the header cells as they are edited. A sheet
/// table covers every row of its sheet and takes its column names from the sheet's column headings; a sheet has
/// either one sheet table or any number of (non-overlapping) region tables.
/// </para>
/// <para>
/// Adding, removing, renaming, resizing and showing/hiding totals are undoable on the table's sheet. Region tables
/// move with row and column inserts and removals like tracked ranges do; removing all of a table marks it deleted.
/// </para>
/// <para>
/// A table on a sheet name the workbook does not have - because the sheet has been removed, or not added yet - is
/// detached: it keeps its region and attaches to the sheet when one with that name is added or renamed to it, if it
/// fits there (within the sheet, not overlapping its tables, and not mixing a sheet table with region tables).
/// One that doesn't fit stays detached. Removing or renaming a detached table can't be undone, as there is no sheet
/// whose undo history it could go in (just as the removal of its sheet can't be undone).
/// </para>
/// <para>
/// <see cref="Workbook.DuplicateSheet"/> does not copy a sheet's tables.
/// </para>
/// </summary>
public sealed class TableCollection : IStructuredReferenceResolver
{
    private readonly Workbook _workbook;

    private sealed record DetachedTable(string SheetName, TableStore.TableState State);

    private readonly Dictionary<string, DetachedTable> _detached = new(StringComparer.OrdinalIgnoreCase);

    // Set while column names are being refreshed, so that a refresh that causes a header change (e.g. by calculating
    // a formula in a header cell) does not start another.
    private bool _isRefreshingColumnNames;

    // Set while the headers changed by a calculation are being read again. Renaming a column recalculates, which may
    // change a header again (e.g. one holding a volatile formula); that change is left for the next calculation
    // rather than recalculating without end.
    private bool _isRefreshingCalculatedHeaders;

    // Tables whose header cells changed while the engine was calculating, read again once it has finished.
    private readonly HashSet<string> _headersChangedWhileCalculating = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Fired after a table is added, removed, renamed, resized or has its totals row shown or hidden (including by
    /// undo and redo), and when editing a table's header cells or a sheet table's column headings renames columns.
    /// </summary>
    public event EventHandler<TableChangedEventArgs>? TableChanged;

    internal TableCollection(Workbook workbook)
    {
        _workbook = workbook;
        _workbook.SheetAdded += (_, args) =>
        {
            Subscribe(args.Sheet);
            Attach(args.Sheet);
        };
        _workbook.SheetRenamed += (_, args) => Attach(args.Sheet);
        _workbook.GetFormulaEngine().CalculationCompleted += (_, args) => RefreshCalculatedHeaders(args.FormulaCount > 0);
    }

    /// <summary>
    /// Adds a table over <paramref name="region"/> on the sheet <paramref name="sheetName"/>. The first row of the
    /// region is the header row and, with <paramref name="showTotals"/>, the last row is the totals row.
    /// Returns null if the name or region is not valid (see <see cref="IsValidTableName"/>) or the sheet does not exist.
    /// </summary>
    public SheetTable? Add(string name, string sheetName, IRegion region, bool showTotals = false)
    {
        var sheet = _workbook.GetSheet(sheetName);
        if (sheet == null)
            return null;

        return sheet.Commands.ExecuteCommand(new AddTableCommand(name, region, showTotals, isSheetTable: false))
            ? Get(name)
            : null;
    }

    /// <summary>
    /// Adds a sheet table to the sheet <paramref name="sheetName"/>: its data is every row of the sheet and its
    /// column names are the sheet's column headings. Returns null if the name is not valid, the sheet does not exist
    /// or already has a table.
    /// </summary>
    public SheetTable? AddSheetTable(string name, string sheetName)
    {
        var sheet = _workbook.GetSheet(sheetName);
        if (sheet == null)
            return null;

        return sheet.Commands.ExecuteCommand(new AddTableCommand(name, null, false, isSheetTable: true))
            ? Get(name)
            : null;
    }

    /// <summary>
    /// Adds a table read from a saved workbook, without a command (so it can't be undone) or a refresh of the
    /// formulas that refer to it. A table on a sheet the workbook doesn't have is detached; its column names are read
    /// when it attaches. A table whose name is taken or not valid, or that doesn't fit its sheet, is skipped.
    /// Returns whether it was added.
    /// </summary>
    internal bool Load(string name, string sheetName, IRegion? region, bool isSheetTable, bool showTotals,
        bool isDeleted)
    {
        if (!isSheetTable && region == null)
            return false;

        // a live region table must have a data row, wherever it is
        if (!isSheetTable && !isDeleted && !HasDataRow(region!, showTotals, out _))
            return false;

        var sheet = _workbook.GetSheet(sheetName);
        if (sheet == null || isDeleted)
        {
            if (!IsValidTableName(name, out _))
                return false;
        }
        else if (!CanAdd(sheet, name, region, isSheetTable, showTotals, out _))
        {
            return false;
        }

        if (sheet == null)
        {
            var state = new TableStore.TableState(TableStore.NextId(), name, isSheetTable,
                !isSheetTable && showTotals, isSheetTable ? null : region!.Clone(), isDeleted && !isSheetTable,
                Array.Empty<string>());
            _detached[name] = new DetachedTable(sheetName, state);
            return true;
        }

        sheet.Tables.AddCore(name, isSheetTable ? null : region, showTotals, isSheetTable,
            isDeleted: isDeleted && !isSheetTable);
        return true;
    }

    /// <summary>
    /// Removes the table <paramref name="name"/>. Returns false if there is no such table.
    /// Removing a detached table can't be undone: it isn't on a sheet, so there is no undo history to hold it.
    /// </summary>
    public bool Remove(string name)
    {
        if (_detached.Remove(name, out var detached))
        {
            // the formulas referring to it are refreshed, as when an attached table is removed
            _workbook.GetFormulaEngine().RefreshTableReferences(detached.State.Name);
            OnTableChanged(new TableChangedEventArgs(detached.State.Name, TableChangeKind.Removed));
            return true;
        }

        var sheet = GetSheetOf(name);
        return sheet != null && sheet.Commands.ExecuteCommand(new RemoveTableCommand(name));
    }

    /// <summary>
    /// Renames the table <paramref name="oldName"/> to <paramref name="newName"/>. Returns false if there is no such
    /// table or the new name is not valid.
    /// Renaming a detached table can't be undone: it isn't on a sheet, so there is no undo history to hold it.
    /// </summary>
    public bool Rename(string oldName, string newName)
    {
        if (_detached.TryGetValue(oldName, out var detached))
        {
            if (!IsValidTableName(newName, oldName, out _))
                return false;

            _detached.Remove(oldName);
            _detached[newName] = detached with { State = detached.State with { Name = newName } };
            _workbook.GetFormulaEngine().RenameTable(detached.State.Name, newName);
            OnTableChanged(new TableChangedEventArgs(newName, TableChangeKind.Renamed, detached.State.Name));
            return true;
        }

        var sheet = GetSheetOf(oldName);
        return sheet != null && sheet.Commands.ExecuteCommand(new RenameTableCommand(oldName, newName));
    }

    /// <summary>
    /// Moves or resizes the region table <paramref name="name"/> to <paramref name="region"/>, which must be on the
    /// same sheet. Returns false if there is no such region table or the region is not valid.
    /// </summary>
    public bool Resize(string name, IRegion region)
    {
        var sheet = GetSheetOf(name);
        return sheet != null && sheet.Commands.ExecuteCommand(new ResizeTableCommand(name, region));
    }

    /// <summary>
    /// Shows or hides the totals row (the last row) of the region table <paramref name="name"/>.
    /// Returns false if there is no such region table, or if showing the totals row would leave it without a data
    /// row.
    /// </summary>
    public bool SetShowTotals(string name, bool showTotals)
    {
        var sheet = GetSheetOf(name);
        return sheet != null && sheet.Commands.ExecuteCommand(new SetTableShowTotalsCommand(name, showTotals));
    }

    /// <summary>
    /// The table <paramref name="name"/> (ignoring case), or null if there is none.
    /// </summary>
    public SheetTable? Get(string name) =>
        _detached.TryGetValue(name, out var detached) ? Snapshot(detached) : GetAttached(name);

    /// <summary>
    /// Whether there is a table named <paramref name="name"/> (ignoring case), including deleted and detached ones.
    /// </summary>
    public bool Contains(string name) =>
        _detached.ContainsKey(name) || _workbook.Sheets.Any(sheet => sheet.Tables.Contains(name));

    /// <summary>
    /// Every table, including deleted and detached ones.
    /// </summary>
    public IEnumerable<SheetTable> GetAll() =>
        _workbook.Sheets.SelectMany(sheet => sheet.Tables.GetAll())
            .Concat(_detached.Values.Select(Snapshot)).ToList();

    /// <summary>
    /// The tables on the sheet <paramref name="sheetName"/>, including deleted and detached ones.
    /// </summary>
    public IEnumerable<SheetTable> GetOnSheet(string sheetName)
    {
        var sheet = _workbook.GetSheet(sheetName);
        var attached = sheet?.Tables.GetAll() ?? Enumerable.Empty<SheetTable>();
        return attached.Concat(_detached.Values.Where(d => d.SheetName == sheetName).Select(Snapshot)).ToList();
    }

    /// <summary>
    /// The table that an unqualified reference (e.g. [@Qty]) in the cell refers to: the region table containing the
    /// cell, otherwise the sheet table of the sheet. Null if there is neither.
    /// </summary>
    public SheetTable? GetTableAt(string sheetName, int row, int col) =>
        _workbook.GetSheet(sheetName)?.Tables.GetTableAt(row, col);

    /// <summary>
    /// Whether <paramref name="name"/> can be used for a new table. A table name must be a valid name (starting with
    /// a letter or underscore, then letters, digits, _ . ? or \), must not look like a cell, row or column address
    /// or a boolean, and must not be used by a variable, function or another table (ignoring case).
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="reason">Why the name can't be used, if it can't.</param>
    public bool IsValidTableName(string name, out string? reason) => IsValidTableName(name, null, out reason);

    /// <summary>
    /// As <see cref="IsValidTableName(string, out string?)"/>, but the table <paramref name="renaming"/> doesn't
    /// count as already using the name.
    /// </summary>
    internal bool IsValidTableName(string name, string? renaming, out string? reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(name) || !RangeText.IsValidName(name))
        {
            reason = $"'{name}' is not a valid name.";
            return false;
        }

        if (string.Equals(name, "TRUE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "FALSE", StringComparison.OrdinalIgnoreCase))
        {
            reason = $"'{name}' is a boolean.";
            return false;
        }

        if (RangeText.TryParseSingleAddress(name, out var address) && address is not NamedAddress)
        {
            reason = $"'{name}' is a cell, row or column address.";
            return false;
        }

        var engine = _workbook.GetFormulaEngine();
        if (engine.GetFunction(name) != null)
        {
            reason = $"'{name}' is the name of a function.";
            return false;
        }

        if (engine.VariableExists(name) ||
            engine.GetVariableNames().Any(v => string.Equals(v, name, StringComparison.OrdinalIgnoreCase)))
        {
            reason = $"'{name}' is the name of a variable.";
            return false;
        }

        var isSelf = renaming != null && string.Equals(name, renaming, StringComparison.OrdinalIgnoreCase);
        if (!isSelf && Contains(name))
        {
            reason = $"A table named '{name}' already exists.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Whether a table named <paramref name="name"/> can be added to <paramref name="sheet"/>, over
    /// <paramref name="region"/> (with a totals row if <paramref name="showTotals"/>) or as a sheet table.
    /// </summary>
    internal bool CanAdd(Sheet sheet, string name, IRegion? region, bool isSheetTable, bool showTotals,
        out string? reason)
    {
        if (!IsValidTableName(name, out reason))
            return false;

        if (isSheetTable)
        {
            if (sheet.Tables.Names.Any())
            {
                reason = $"Sheet '{sheet.Name}' already has a table.";
                return false;
            }

            return true;
        }

        if (region == null)
        {
            reason = "A table needs a region.";
            return false;
        }

        return IsValidRegion(sheet, null, region, showTotals, out reason);
    }

    /// <summary>
    /// Whether the region table <paramref name="name"/> on <paramref name="sheet"/> can be moved to
    /// <paramref name="region"/>.
    /// </summary>
    internal bool CanResize(Sheet sheet, string name, IRegion region, out string? reason)
    {
        var table = sheet.Tables.Get(name);
        if (table == null || table.IsSheetTable)
        {
            reason = $"There is no region table named '{name}' on sheet '{sheet.Name}'.";
            return false;
        }

        return IsValidRegion(sheet, table.Name, region, table.ShowTotals, out reason);
    }

    /// <summary>
    /// Whether the totals row of the region table <paramref name="name"/> on <paramref name="sheet"/> can be shown
    /// or hidden. Showing it must leave at least one data row.
    /// </summary>
    internal bool CanSetShowTotals(Sheet sheet, string name, bool showTotals, out string? reason)
    {
        var table = sheet.Tables.Get(name);
        if (table == null || table.IsSheetTable)
        {
            reason = $"There is no region table named '{name}' on sheet '{sheet.Name}'.";
            return false;
        }

        reason = null;
        // a deleted table has no rows to keep; its region is checked when it is restored or resized
        return !showTotals || table.IsDeleted || HasDataRow(table.Region!, true, out reason);
    }

    /// <summary>
    /// Whether a region table over <paramref name="region"/> has a data row: a header row, at least one data row and,
    /// with <paramref name="showTotals"/>, a totals row.
    /// </summary>
    private static bool HasDataRow(IRegion region, bool showTotals, out string? reason)
    {
        reason = null;
        var minHeight = showTotals ? 3 : 2;
        if (region.Height >= minHeight)
            return true;

        reason = showTotals
            ? "A table with a totals row must have a header row, at least one data row and the totals row."
            : "A table must have a header row and at least one data row.";
        return false;
    }

    private static bool IsValidRegion(Sheet sheet, string? except, IRegion region, bool showTotals,
        out string? reason)
    {
        reason = null;
        // whole columns or rows would pin the header to the sheet's edge; a sheet table covers whole columns
        if (region is ColumnRegion or RowRegion)
        {
            reason = "A table must be a region with a fixed number of rows and columns.";
            return false;
        }

        if (region.Top < 0 || region.Left < 0 || region.Bottom >= sheet.NumRows || region.Right >= sheet.NumCols)
        {
            reason = "The table must be within the sheet.";
            return false;
        }

        if (!HasDataRow(region, showTotals, out reason))
            return false;

        foreach (var other in sheet.Tables.GetAll())
        {
            if (except != null && string.Equals(other.Name, except, StringComparison.OrdinalIgnoreCase))
                continue;

            if (other.IsSheetTable)
            {
                reason = $"Sheet '{sheet.Name}' has a sheet table.";
                return false;
            }

            if (!other.IsDeleted && other.Region!.Intersects(region))
            {
                reason = $"The table would overlap table '{other.Name}'.";
                return false;
            }
        }

        return true;
    }

    public StructuredReferenceResolution Resolve(StructuredReferenceDescriptor descriptor, int? callerRow,
        int? callerCol, string? callerSheet)
    {
        // without a calling cell there is no row for @ to refer to
        if (descriptor.IsThisRow && (callerRow == null || callerCol == null || callerSheet == null))
            return StructuredReferenceResolution.FromError(ErrorType.Value);

        SheetTable? table;
        if (descriptor.TableName != null)
            table = GetAttached(descriptor.TableName);
        else if (callerRow != null && callerCol != null && callerSheet != null)
            table = GetTableAt(callerSheet, callerRow.Value, callerCol.Value);
        else
            table = null;

        if (table == null)
            return StructuredReferenceResolution.FromError(ErrorType.Ref);

        return table.Resolve(descriptor, callerRow, callerSheet);
    }

    /// <summary>
    /// The names of the tables that can be referred to: not deleted or detached.
    /// </summary>
    public IEnumerable<string> GetTableNames() =>
        _workbook.Sheets.SelectMany(sheet => sheet.Tables.GetAll()).Where(t => !t.IsDeleted).Select(t => t.Name)
            .ToList();

    internal void OnTableChanged(TableChangedEventArgs args) => TableChanged?.Invoke(this, args);

    private SheetTable? GetAttached(string name)
    {
        foreach (var sheet in _workbook.Sheets)
        {
            if (sheet.Tables.Get(name) is { } table)
                return table;
        }

        return null;
    }

    private Sheet? GetSheetOf(string name) => _workbook.Sheets.FirstOrDefault(sheet => sheet.Tables.Contains(name));

    private static SheetTable Snapshot(DetachedTable detached)
    {
        var state = detached.State;
        return state.IsSheetTable
            ? SheetTable.ForSheet(state.Name, detached.SheetName, 0, state.ColumnNames)
            : SheetTable.ForRegion(state.Name, detached.SheetName, state.Region!, state.ShowTotals, state.IsDeleted,
                state.ColumnNames);
    }

    private void Attach(Sheet sheet)
    {
        var attaching = _detached.Where(d => d.Value.SheetName == sheet.Name).ToList();
        if (attaching.Count == 0)
            return;

        // the formulas that refer to the tables are recalculated once, for all of them
        var engine = _workbook.GetFormulaEngine();
        using (engine.SuspendCalculation())
        {
            foreach (var (name, detached) in attaching)
            {
                // A table that doesn't fit the sheet (e.g. it is smaller, or has tables where this one would go) is
                // left detached, as it would be with no sheet of the name: it keeps its name and region, and
                // attaches to a later sheet of the name that it fits. Checked one by one, so that tables attached
                // earlier count.
                if (!CanAttach(sheet, detached.State))
                    continue;

                _detached.Remove(name);
                sheet.Tables.RestoreCore(detached.State);
                engine.RefreshTableReferences(detached.State.Name);
            }
        }
    }

    /// <summary>
    /// Whether the detached table <paramref name="state"/> can be put on <paramref name="sheet"/>, as
    /// <see cref="CanAdd"/> checks for a new table. Its name is its own, so isn't checked.
    /// </summary>
    private static bool CanAttach(Sheet sheet, TableStore.TableState state)
    {
        if (state.IsSheetTable)
            return !sheet.Tables.Names.Any();

        // a deleted table occupies no cells, but still can't share a sheet with a sheet table
        if (state.IsDeleted || state.Region == null)
            return !sheet.Tables.GetAll().Any(t => t.IsSheetTable);

        return IsValidRegion(sheet, null, state.Region, state.ShowTotals, out _);
    }

    /// <summary>
    /// Detaches the tables of a sheet that has been taken out of the workbook. Called by
    /// <see cref="Workbook.RemoveSheet"/> while calculation is suspended, so that the formulas referring to the
    /// tables are recalculated by the one calculation that follows the removal.
    /// </summary>
    internal void OnSheetRemoved(Sheet sheet)
    {
        Unsubscribe(sheet);

        var engine = _workbook.GetFormulaEngine();
        using (engine.SuspendCalculation())
        {
            foreach (var name in sheet.Tables.Names.ToList())
            {
                var state = sheet.Tables.RemoveCore(name);
                if (state != null)
                    _detached[state.Name] = new DetachedTable(sheet.Name, state);
                engine.RefreshTableReferences(name);
            }
        }
    }

    private readonly Dictionary<Sheet, SheetSubscription> _subscriptions = new();

    /// <summary>
    /// The handlers attached to a sheet, kept so that they can be detached when it is removed.
    /// </summary>
    private sealed record SheetSubscription(
        EventHandler<CellDataChangedEventArgs> CellsChanged,
        EventHandler<HeadingsModifiedEventArgs> HeadingsModified,
        EventHandler<CommandRunEventArgs> CommandRun,
        EventHandler<UndoCommandRunEventArgs> CommandUndone);

    private void Subscribe(Sheet sheet)
    {
        if (_subscriptions.ContainsKey(sheet))
            return;

        var subscription = new SheetSubscription(
            (_, args) => OnCellsChanged(sheet, args),
            (_, args) => OnHeadingsModified(sheet, args),
            (_, args) => OnCommandRunOrUndone(sheet, args.Command),
            (_, args) => OnCommandRunOrUndone(sheet, args.Command));

        sheet.Cells.CellsChanged += subscription.CellsChanged;
        sheet.Columns.HeadingsModified += subscription.HeadingsModified;
        sheet.Commands.CommandRun += subscription.CommandRun;
        sheet.Commands.CommandUndone += subscription.CommandUndone;
        _subscriptions[sheet] = subscription;
    }

    private void Unsubscribe(Sheet sheet)
    {
        if (!_subscriptions.Remove(sheet, out var subscription))
            return;

        sheet.Cells.CellsChanged -= subscription.CellsChanged;
        sheet.Columns.HeadingsModified -= subscription.HeadingsModified;
        sheet.Commands.CommandRun -= subscription.CommandRun;
        sheet.Commands.CommandUndone -= subscription.CommandUndone;
    }

    private void OnCommandRunOrUndone(Sheet sheet, ICommand command)
    {
        // cheap unless the command (or its undo) moved or resized tables: the store records when it has
        sheet.Tables.ResyncColumnNames();

        // A header's displayed text, and so its column name, depends on its number format. Format changes don't
        // raise CellsChanged, so the headers a format command covers are read again here (after running or undoing).
        var formatRegion = command switch
        {
            SetFormatCommand set => set.Region,
            ClearFormatCommand clear => clear.Region,
            _ => null
        };

        if (formatRegion == null || _isRefreshingColumnNames || !sheet.Tables.Names.Any())
            return;

        foreach (var name in sheet.Tables.GetNamesWithHeaderIn([formatRegion], []))
            RefreshColumnNames(sheet, name);
    }

    private void OnCellsChanged(Sheet sheet, CellDataChangedEventArgs args)
    {
        if (_isRefreshingColumnNames || !sheet.Tables.Names.Any())
            return;

        var calculating = _workbook.GetFormulaEngine().IsCalculating;

        var changed = sheet.Tables.GetNamesWithHeaderIn(args.Regions, args.Positions);

        foreach (var name in changed)
        {
            // read once the calculation has finished, with the headers it calculated
            if (calculating)
                _headersChangedWhileCalculating.Add(name);
            else
                RefreshColumnNames(sheet, name);
        }
    }

    /// <summary>
    /// Reads again the headers that changed while calculating and, if the calculation evaluated any formulas, every
    /// header holding a formula - whose change is not always raised (e.g. when the calculation runs inside another
    /// handler of a batch's changes). The renames are applied with one recalculation, whose own header changes are
    /// left for the next calculation.
    /// </summary>
    /// <param name="evaluatedFormulas">Whether the calculation evaluated any formulas. A header formula that wasn't
    /// evaluated can't have changed.</param>
    private void RefreshCalculatedHeaders(bool evaluatedFormulas)
    {
        if (_isRefreshingColumnNames || _isRefreshingCalculatedHeaders)
            return;

        var changed = new List<(Sheet Sheet, string Name)>();
        foreach (var name in _headersChangedWhileCalculating)
        {
            if (GetSheetOf(name) is { } sheet)
                changed.Add((sheet, name));
        }

        _headersChangedWhileCalculating.Clear();

        foreach (var sheet in evaluatedFormulas ? _workbook.Sheets : Enumerable.Empty<Sheet>())
        {
            foreach (var name in sheet.Tables.GetNamesWithHeaderFormulas())
            {
                if (!changed.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
                    changed.Add((sheet, name));
            }
        }

        if (changed.Count == 0)
            return;

        _isRefreshingCalculatedHeaders = true;
        try
        {
            using (_workbook.GetFormulaEngine().SuspendCalculation())
            {
                foreach (var (sheet, name) in changed)
                    RefreshColumnNames(sheet, name);
            }
        }
        finally
        {
            _isRefreshingCalculatedHeaders = false;
        }
    }

    /// <summary>
    /// A sheet table's column names, and so its #Headers values, are the sheet's column headings - so the formulas
    /// that refer to it are refreshed when they change, whether or not a column was renamed.
    /// </summary>
    private void OnHeadingsModified(Sheet sheet, HeadingsModifiedEventArgs args)
    {
        if (_isRefreshingColumnNames || args.Axis != Axis.Col)
            return;

        foreach (var table in sheet.Tables.GetAll().Where(t => t.IsSheetTable).ToList())
        {
            if (!RefreshColumnNames(sheet, table.Name))
                _workbook.GetFormulaEngine().RefreshTableReferences(table.Name);
        }
    }

    /// <summary>
    /// Re-reads the column names of a table whose header changed, and raises <see cref="TableChangeKind.ColumnsRenamed"/>
    /// if any were renamed. Names that changed because the table moved or changed size are not renames.
    /// Returns whether any were renamed, in which case the formulas that refer to the table have been refreshed.
    /// </summary>
    private bool RefreshColumnNames(Sheet sheet, string name)
    {
        List<(string OldName, string NewName)> renames;
        _isRefreshingColumnNames = true;
        try
        {
            var shapeChanged = sheet.Tables.ColumnShapeChanged(name);
            var (oldNames, newNames) = sheet.Tables.RefreshColumnNames(name);
            if (shapeChanged || oldNames.Count != newNames.Count)
                return false;

            renames = new List<(string OldName, string NewName)>();
            for (var i = 0; i < oldNames.Count; i++)
            {
                // surrounding whitespace is not part of a name in a reference, so changing it is not a rename
                if (!oldNames[i].AsSpan().Trim().Equals(newNames[i].AsSpan().Trim(), StringComparison.Ordinal))
                    renames.Add((oldNames[i], newNames[i]));
            }
        }
        finally
        {
            _isRefreshingColumnNames = false;
        }

        if (renames.Count == 0)
            return false;

        // Outside the guard, because the recalculation this starts may change a header cell holding a formula,
        // which must be read again. The engine defers the rename if it is called while calculating.
        var tableName = sheet.Tables.Get(name)?.Name ?? name;
        _workbook.GetFormulaEngine().RenameTableColumns(tableName, renames);
        OnTableChanged(new TableChangedEventArgs(tableName, TableChangeKind.ColumnsRenamed,
            columnRenames: renames));
        return true;
    }
}
