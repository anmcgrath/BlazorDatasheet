using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.Store;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// The tracked ranges on one sheet. Shifted by row/column inserts and removals through the sheet's shifting-store
/// registry, so undo and redo restore them with everything else. Use <see cref="Workbook.TrackedRanges"/> to read and
/// write them.
/// </summary>
internal sealed class TrackedRangeStore : IRowColShiftingStore
{
    private readonly Sheet _sheet;

    // Inserting just after a range does not grow it; inserting inside it does. A range wholly removed leaves the store,
    // which is what marks it deleted.
    private readonly RegionDataStore<Entry> _store = new(minArea: 0, expandWhenInsertAfter: false);

    // The current entry for each key tracked on this sheet. An entry the store holds under an older id (put back by
    // undoing a structural change made before the key was re-tracked or untracked) is stale and is discarded.
    private readonly Dictionary<string, long> _live = new(StringComparer.Ordinal);

    // Keys whose range was removed, with the region they last occupied.
    private readonly Dictionary<string, IRegion> _deleted = new(StringComparer.Ordinal);

    private static long _nextId;

    public TrackedRangeStore(Sheet sheet)
    {
        _sheet = sheet;
    }

    internal readonly record struct Entry(string Key, long Id);

    private sealed class RestoreData
    {
        public required RegionRestoreData<Entry> Store { get; init; }
        public List<Entry> Deleted { get; } = new();
    }

    public bool Contains(string key) => _live.ContainsKey(key);

    public IEnumerable<string> Keys => _live.Keys;

    public void Track(string key, IRegion region, bool isDeleted = false)
    {
        Untrack(key);
        var entry = new Entry(key, Interlocked.Increment(ref _nextId));
        _live[key] = entry.Id;

        if (isDeleted)
            _deleted[key] = region.Clone();
        else
            _store.Add(region.Clone(), entry);
    }

    public bool Untrack(string key)
    {
        if (!_live.Remove(key, out var id))
            return false;

        _deleted.Remove(key);
        foreach (var dataRegion in _store.GetAllDataRegions().Where(d => d.Data.Id == id).ToList())
            _store.Delete(dataRegion);

        return true;
    }

    public TrackedRange? Get(string key)
    {
        if (!_live.TryGetValue(key, out var id))
            return null;

        if (_deleted.TryGetValue(key, out var lastRegion))
            return new TrackedRange(key, _sheet.Name, lastRegion.Clone(), IsDeleted: true);

        var dataRegion = _store.GetAllDataRegions().FirstOrDefault(d => d.Data.Id == id);
        return dataRegion == null
            ? null
            : new TrackedRange(key, _sheet.Name, dataRegion.Region.Clone(), IsDeleted: false);
    }

    public IEnumerable<TrackedRange> GetAll() =>
        _live.Keys.Select(Get).OfType<TrackedRange>().ToList();

    public object? InsertRowColAt(int index, int count, Axis axis) =>
        new RestoreData { Store = _store.InsertRowColAt(index, count, axis) };

    public object? RemoveRowColAt(int index, int count, Axis axis)
    {
        var restoreData = new RestoreData { Store = _store.RemoveRowColAt(index, count, axis) };

        var remaining = restoreData.Store.RegionsAdded.Select(d => d.Data).ToHashSet();
        foreach (var removed in restoreData.Store.RegionsRemoved)
        {
            if (remaining.Contains(removed.Data) || !IsLive(removed.Data))
                continue;

            _deleted[removed.Data.Key] = removed.Region.Clone();
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
            _deleted.Remove(entry.Key);

        // Undo puts back what the structural change took, including entries since untracked or re-tracked.
        var stale = _store.GetAllDataRegions().Where(d => !IsLive(d.Data) || _deleted.ContainsKey(d.Data.Key))
            .ToList();
        foreach (var dataRegion in stale)
            _store.Delete(dataRegion);
    }

    private bool IsLive(Entry entry) => _live.TryGetValue(entry.Key, out var id) && id == entry.Id;
}
