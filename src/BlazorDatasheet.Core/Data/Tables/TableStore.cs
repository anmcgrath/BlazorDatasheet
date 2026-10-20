using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// The tables on one sheet. Region tables are shifted by row/column inserts and removals through the sheet's
/// shifting-store registry, so undo and redo restore them with everything else. A sheet table has no region of its
/// own: it is always sized to the sheet. Use <see cref="Workbook.Tables"/> to read and write tables; the mutators here
/// are not undoable.
/// </summary>
internal sealed class TableStore : IRowColShiftingStore
{
    private readonly Sheet _sheet;

    // Inserting just after a table does not grow it; inserting inside it does. A table wholly removed leaves the
    // store, which is what marks it deleted. Entries are keyed by id rather than name so that renaming a table
    // doesn't orphan the entries held by restore data. Each entry also has an id of its own, so that an entry put
    // back by undo can be told apart from one added since for the same table (e.g. by undoing its removal).
    private readonly RegionDataStore<Entry> _store = new(minArea: 0, expandWhenInsertAfter: false);

    // The current tables on this sheet, by name and by id. An entry the store holds under an id that isn't here (put
    // back by undoing a structural change made before the table was removed), or that isn't the table's current
    // entry, is stale and is discarded.
    private readonly Dictionary<string, TableInfo> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, TableInfo> _byId = new();

    // Tables whose region was removed, with the region they last occupied.
    private readonly Dictionary<long, IRegion> _deleted = new();

    private static long _nextId;
    private static long _nextEntryId;

    // Bumped by every change to the tables or the store, which makes the cached snapshots and regions out of date.
    private long _version;

    // The region of each live table's current entry, by table id, built when first needed after a change. Saves
    // scanning the store each time a table's region is read (e.g. for every structured reference evaluated).
    private Dictionary<long, IRegion>? _regions;

    // Set when the tables may have moved or changed size since their column names were last resynced, and the number
    // of columns (which a sheet table spans) they were resynced with, so that a resync can be skipped when neither
    // has changed.
    private bool _shapesMayHaveChanged;
    private int _resyncedNumCols = -1;

    public TableStore(Sheet sheet)
    {
        _sheet = sheet;
    }

    internal readonly record struct Entry(long Id, long EntryId);

    /// <summary>
    /// A table's metadata. The column names are cached, along with the shape (header row and columns) they were read
    /// from, so that a change of names can be told apart from a change of shape.
    /// </summary>
    internal sealed class TableInfo
    {
        public required long Id { get; init; }
        public required string Name { get; set; }
        public required bool IsSheetTable { get; init; }

        /// <summary>
        /// The id of the table's entry in the store. Entries for the table with any other entry id are stale.
        /// </summary>
        public long EntryId { get; set; }

        public bool ShowTotals { get; set; }
        public IReadOnlyList<string> ColumnNames { get; set; } = Array.Empty<string>();
        public (int Top, int Left, int Right)? ColumnNamesShape { get; set; }

        /// <summary>
        /// The last snapshot taken of the table, or null if there is none or it is known to be out of date.
        /// </summary>
        public CachedSnapshot? Snapshot { get; set; }
    }

    /// <summary>
    /// A snapshot of a table, valid while the store's version and the sheet's name and size are as they were when it
    /// was taken.
    /// </summary>
    internal sealed record CachedSnapshot(SheetTable Table, long Version, string SheetName, int NumRows, int NumCols);

    /// <summary>
    /// Everything needed to put a removed table back exactly as it was.
    /// </summary>
    internal sealed record TableState(
        long Id,
        string Name,
        bool IsSheetTable,
        bool ShowTotals,
        IRegion? Region,
        bool IsDeleted,
        IReadOnlyList<string> ColumnNames);

    private sealed class RestoreData
    {
        public required RegionRestoreData<Entry> Store { get; init; }
        public List<Entry> Deleted { get; } = new();
    }

    public bool Contains(string name) => _byName.ContainsKey(name);

    public IEnumerable<string> Names => _byName.Values.Select(t => t.Name);

    /// <summary>
    /// Adds a table. A region table needs <paramref name="region"/>; a sheet table ignores it. Pass
    /// <paramref name="id"/> to put back a table that was removed (or added, then undone) so that restore data
    /// holding its entries still applies. Returns the table's id.
    /// </summary>
    public long AddCore(string name, IRegion? region, bool showTotals, bool isSheetTable, long? id = null,
        bool isDeleted = false, IReadOnlyList<string>? columnNames = null)
    {
        if (_byName.ContainsKey(name))
            throw new InvalidOperationException($"Table '{name}' already exists on sheet '{_sheet.Name}'.");
        if (!isSheetTable && region == null)
            throw new ArgumentNullException(nameof(region));

        var info = new TableInfo
        {
            Id = id ?? NextId(),
            Name = name,
            IsSheetTable = isSheetTable,
            ShowTotals = !isSheetTable && showTotals,
            ColumnNames = columnNames ?? Array.Empty<string>()
        };

        _byName[name] = info;
        _byId[info.Id] = info;

        if (!isSheetTable)
        {
            if (isDeleted)
                _deleted[info.Id] = region!.Clone();
            else
                AddEntry(info, region!);
        }

        // given names are kept as they are (e.g. while detached), so a later change of header can still be seen
        if (columnNames != null && !isDeleted)
            info.ColumnNamesShape = GetShape(info);
        else if (!isDeleted)
            RefreshColumnNames(info);

        Invalidate();
        return info.Id;
    }

    /// <summary>
    /// Puts back a table removed with <see cref="RemoveCore"/>.
    /// A state without column names (a detached table loaded from a saved workbook) has them read again.
    /// </summary>
    public void RestoreCore(TableState state) =>
        AddCore(state.Name, state.Region, state.ShowTotals, state.IsSheetTable, state.Id, state.IsDeleted,
            state.ColumnNames.Count == 0 ? null : state.ColumnNames);

    /// <summary>
    /// A new table id, unique across the workbook's sheets.
    /// </summary>
    internal static long NextId() => Interlocked.Increment(ref _nextId);

    /// <summary>
    /// Removes the table <paramref name="name"/>, returning what is needed to put it back, or null if there is no such
    /// table.
    /// </summary>
    public TableState? RemoveCore(string name)
    {
        if (!_byName.TryGetValue(name, out var info))
            return null;

        var state = GetState(info);
        _byName.Remove(name);
        _byId.Remove(info.Id);
        _deleted.Remove(info.Id);
        RemoveEntries(info.Id);
        Invalidate();
        return state;
    }

    public bool RenameCore(string oldName, string newName)
    {
        if (!_byName.TryGetValue(oldName, out var info))
            return false;

        if (_byName.TryGetValue(newName, out var existing) && existing != info)
            throw new InvalidOperationException($"Table '{newName}' already exists on sheet '{_sheet.Name}'.");

        _byName.Remove(oldName);
        info.Name = newName;
        _byName[newName] = info;
        Invalidate();
        return true;
    }

    /// <summary>
    /// Moves a region table to <paramref name="region"/>, un-deleting it if it was deleted. With
    /// <paramref name="isDeleted"/> it is left deleted, having last occupied <paramref name="region"/>.
    /// </summary>
    public bool SetRegionCore(string name, IRegion region, bool isDeleted = false)
    {
        if (!_byName.TryGetValue(name, out var info) || info.IsSheetTable)
            return false;

        RemoveEntries(info.Id);
        _deleted.Remove(info.Id);

        if (isDeleted)
            _deleted[info.Id] = region.Clone();
        else
            AddEntry(info, region);

        _shapesMayHaveChanged = true;
        Invalidate();
        return true;
    }

    public bool SetShowTotalsCore(string name, bool showTotals)
    {
        if (!_byName.TryGetValue(name, out var info) || info.IsSheetTable)
            return false;

        info.ShowTotals = showTotals;
        Invalidate();
        return true;
    }

    /// <summary>
    /// The table <paramref name="name"/> (ignoring case), or null if there is none on this sheet.
    /// </summary>
    public SheetTable? Get(string name) =>
        _byName.TryGetValue(name, out var info) ? Snapshot(info) : null;

    /// <summary>
    /// Every table on this sheet, including deleted ones.
    /// </summary>
    public IEnumerable<SheetTable> GetAll() => _byName.Values.Select(Snapshot).OfType<SheetTable>().ToList();

    /// <summary>
    /// The table that an unqualified reference in the cell refers to: the region table containing it, otherwise the
    /// sheet's sheet table. Null if there is neither.
    /// </summary>
    public SheetTable? GetTableAt(int row, int col)
    {
        foreach (var entry in _store.GetData(row, col))
        {
            if (IsCurrent(entry))
                return Snapshot(_byId[entry.Id]);
        }

        var sheetTable = _byName.Values.FirstOrDefault(t => t.IsSheetTable);
        return sheetTable == null ? null : Snapshot(sheetTable);
    }

    /// <summary>
    /// The names of the region tables, not deleted, that have a formula in a header cell. Reads the sheet's formulas
    /// directly rather than taking snapshots, as it is called after every calculation.
    /// </summary>
    public List<string> GetNamesWithHeaderFormulas()
    {
        var names = new List<string>();
        if (_byName.Count == 0)
            return names;

        var formulas = _sheet.Cells.GetFormulaStore();
        foreach (var dataRegion in _store.GetAllDataRegions())
        {
            if (!IsCurrent(dataRegion.Data) || _deleted.ContainsKey(dataRegion.Data.Id))
                continue;

            var info = _byId[dataRegion.Data.Id];

            var region = dataRegion.Region;
            if (formulas.GetNonEmptyData(new Region(region.Top, region.Top, region.Left, region.Right)).Any())
                names.Add(info.Name);
        }

        return names;
    }

    /// <summary>
    /// The names of the region tables, not deleted, whose header row intersects any of <paramref name="regions"/> or
    /// contains any of <paramref name="positions"/>. Looks the tables up spatially rather than taking snapshots, as it
    /// is called on every change of cells.
    /// </summary>
    public List<string> GetNamesWithHeaderIn(IEnumerable<IRegion> regions, IEnumerable<CellPosition> positions)
    {
        var names = new List<string>();
        if (_byName.Count == 0 || !_store.Any())
            return names;

        var found = new HashSet<long>();

        void Add(Entry entry)
        {
            if (IsCurrent(entry) && !_deleted.ContainsKey(entry.Id) && found.Add(entry.Id))
                names.Add(_byId[entry.Id].Name);
        }

        foreach (var region in regions)
        {
            foreach (var dataRegion in _store.GetDataRegions(region))
            {
                var table = dataRegion.Region;
                if (region.Intersects(new Region(table.Top, table.Top, table.Left, table.Right)))
                    Add(dataRegion.Data);
            }
        }

        foreach (var position in positions)
        {
            foreach (var dataRegion in _store.GetDataRegions(position.row, position.col))
            {
                if (dataRegion.Region.Top == position.row)
                    Add(dataRegion.Data);
            }
        }

        return names;
    }

    internal TableState? GetState(string name) =>
        _byName.TryGetValue(name, out var info) ? GetState(info) : null;

    private TableState GetState(TableInfo info) =>
        new(info.Id, info.Name, info.IsSheetTable, info.ShowTotals, GetRegion(info)?.Clone(),
            _deleted.ContainsKey(info.Id), info.ColumnNames);

    /// <summary>
    /// Re-reads the column names of table <paramref name="name"/> from its header cells (a region table) or the sheet's
    /// column headings (a sheet table), returning the names before and after. A deleted table keeps its names.
    /// </summary>
    public (IReadOnlyList<string> OldNames, IReadOnlyList<string> NewNames) RefreshColumnNames(string name)
    {
        if (!_byName.TryGetValue(name, out var info))
            return (Array.Empty<string>(), Array.Empty<string>());

        var old = info.ColumnNames;
        RefreshColumnNames(info);
        return (old, info.ColumnNames);
    }

    /// <summary>
    /// Whether the table's header row or columns have changed (moved, grown or shrunk) since its column names were
    /// last read. Names that differ after such a change are not renames.
    /// </summary>
    public bool ColumnShapeChanged(string name) =>
        _byName.TryGetValue(name, out var info) && !_deleted.ContainsKey(info.Id) &&
        info.ColumnNamesShape != GetShape(info);

    /// <summary>
    /// Re-reads the column names of every table that has moved or changed size since they were last read, so that a
    /// later change to the header is compared with names read from the same cells.
    /// </summary>
    public void ResyncColumnNames()
    {
        // only a structural change (or undoing one) or a resize moves tables, and only a change in the number of
        // columns changes a sheet table's shape, so there is nothing to do after any other change
        if (!_shapesMayHaveChanged && _resyncedNumCols == _sheet.NumCols)
            return;

        _shapesMayHaveChanged = false;
        _resyncedNumCols = _sheet.NumCols;

        foreach (var info in _byId.Values)
        {
            if (!_deleted.ContainsKey(info.Id) && info.ColumnNamesShape != GetShape(info))
                RefreshColumnNames(info);
        }
    }

    private void RefreshColumnNames(TableInfo info)
    {
        if (_deleted.ContainsKey(info.Id))
            return;

        var shape = GetShape(info);
        if (shape == null)
            return;

        IEnumerable<string?> raw;
        if (info.IsSheetTable)
        {
            raw = Enumerable.Range(0, _sheet.NumCols).Select(col => _sheet.Columns.GetHeading(col));
        }
        else
        {
            var (top, left, right) = shape.Value;
            raw = Enumerable.Range(left, right - left + 1)
                .Select(col => CellValueText.ToDisplayText(_sheet.Cells.GetCellValue(top, col),
                    _sheet.GetFormatForRendering(top, col)));
        }

        info.ColumnNames = SheetTable.BuildColumnNames(raw);
        info.ColumnNamesShape = shape;
        info.Snapshot = null;
    }

    private (int Top, int Left, int Right)? GetShape(TableInfo info)
    {
        if (info.IsSheetTable)
            return (-1, 0, _sheet.NumCols - 1);

        var region = GetRegion(info);
        return region == null ? null : (region.Top, region.Left, region.Right);
    }

    private IRegion? GetRegion(TableInfo info)
    {
        if (info.IsSheetTable)
            return null;
        if (_deleted.TryGetValue(info.Id, out var lastRegion))
            return lastRegion;

        if (_regions == null)
        {
            _regions = new Dictionary<long, IRegion>();
            foreach (var dataRegion in _store.GetAllDataRegions())
            {
                if (IsCurrent(dataRegion.Data))
                    _regions[dataRegion.Data.Id] = dataRegion.Region;
            }
        }

        return _regions.GetValueOrDefault(info.Id);
    }

    private SheetTable? Snapshot(TableInfo info)
    {
        // A snapshot is immutable and only changes with the table, so it is taken again only after a change. A sheet
        // table's size follows the sheet's, and every snapshot carries the sheet's name, so those are checked too.
        if (info.Snapshot is { } cached && cached.Version == _version && cached.SheetName == _sheet.Name &&
            cached.NumRows == _sheet.NumRows && cached.NumCols == _sheet.NumCols)
            return cached.Table;

        var isDeleted = _deleted.ContainsKey(info.Id);

        // names read before the table moved or changed size may be out of date
        if (!isDeleted && info.ColumnNamesShape != GetShape(info))
            RefreshColumnNames(info);

        SheetTable? table;
        if (info.IsSheetTable)
        {
            table = SheetTable.ForSheet(info.Name, _sheet.Name, _sheet.NumRows, info.ColumnNames);
        }
        else
        {
            var region = GetRegion(info);
            table = region == null
                ? null
                : SheetTable.ForRegion(info.Name, _sheet.Name, region, info.ShowTotals, isDeleted,
                    info.ColumnNames);
        }

        info.Snapshot = table == null
            ? null
            : new CachedSnapshot(table, _version, _sheet.Name, _sheet.NumRows, _sheet.NumCols);
        return table;
    }

    /// <summary>
    /// Marks the cached snapshots and regions out of date. Called by everything that changes a table or the store.
    /// </summary>
    private void Invalidate()
    {
        _version++;
        _regions = null;
    }

    /// <summary>
    /// Adds a new entry for the table, which becomes its current entry.
    /// </summary>
    private void AddEntry(TableInfo info, IRegion region)
    {
        info.EntryId = Interlocked.Increment(ref _nextEntryId);
        _store.Add(region.Clone(), new Entry(info.Id, info.EntryId));
        Invalidate();
    }

    private void RemoveEntries(long id)
    {
        foreach (var dataRegion in _store.GetAllDataRegions().Where(d => d.Data.Id == id).ToList())
            _store.Delete(dataRegion);
        Invalidate();
    }

    public object? InsertRowColAt(int index, int count, Axis axis) =>
        Shifted(new RestoreData { Store = _store.InsertRowColAt(index, count, axis) });

    public object? RemoveRowColAt(int index, int count, Axis axis) =>
        Shifted(WithDeletedDetection(_store.RemoveRowColAt(index, count, axis)));

    // A table straddling the band is kept as it is, as Excel does with references.
    public object? InsertCellsAt(IRegion region, Axis axis) =>
        Shifted(new RestoreData { Store = _store.InsertCellsAt(region, axis, StraddlePolicy.Keep) });

    public object? RemoveCellsAt(IRegion region, Axis axis) =>
        Shifted(WithDeletedDetection(_store.RemoveCellsAt(region, axis, StraddlePolicy.Keep)));

    /// <summary>
    /// Records that the tables may have moved, changed size or been deleted, returning <paramref name="restoreData"/>.
    /// </summary>
    private RestoreData Shifted(RestoreData restoreData)
    {
        _shapesMayHaveChanged = true;
        Invalidate();
        return restoreData;
    }

    /// <summary>
    /// Marks deleted every live table that a removal took out of the store without putting a shifted or contracted
    /// version back.
    /// </summary>
    private RestoreData WithDeletedDetection(RegionRestoreData<Entry> storeRestoreData)
    {
        var restoreData = new RestoreData { Store = storeRestoreData };

        var remaining = restoreData.Store.RegionsAdded.Select(d => d.Data).ToHashSet();
        foreach (var removed in restoreData.Store.RegionsRemoved)
        {
            if (remaining.Contains(removed.Data) || !IsLive(removed.Data))
                continue;

            _deleted[removed.Data.Id] = removed.Region.Clone();
            restoreData.Deleted.Add(removed.Data);
        }

        return restoreData;
    }

    public void Restore(object? restoreData)
    {
        if (restoreData is not RestoreData data)
            return;

        _store.Restore(data.Store);

        foreach (var entry in data.Deleted.Where(IsLive))
            _deleted.Remove(entry.Id);

        // The entries put back are the tables as they were before the structural change, so they win over any entry
        // added for the same table since (e.g. by undoing the table's removal or resize at its shifted region).
        foreach (var dataRegion in data.Store.RegionsRemoved)
        {
            if (_byId.TryGetValue(dataRegion.Data.Id, out var info) && !_deleted.ContainsKey(info.Id))
                info.EntryId = dataRegion.Data.EntryId;
        }

        // Undo puts back what the structural change took, including entries of tables since removed or deleted.
        var stale = _store.GetAllDataRegions().Where(d => !IsCurrent(d.Data) || _deleted.ContainsKey(d.Data.Id))
            .ToList();
        foreach (var dataRegion in stale)
            _store.Delete(dataRegion);

        _shapesMayHaveChanged = true;
        Invalidate();
    }

    private bool IsLive(Entry entry) => _byId.ContainsKey(entry.Id);

    private bool IsCurrent(Entry entry) => _byId.TryGetValue(entry.Id, out var info) && info.EntryId == entry.EntryId;
}
