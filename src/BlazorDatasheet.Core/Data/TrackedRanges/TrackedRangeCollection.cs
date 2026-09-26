using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Ranges tracked under a key through row and column inserts and removals, and their undo and redo.
/// A tracked range is not a formula: it is never calculated and cannot be referenced by a formula.
/// <para>
/// Inserting inside a range grows it; inserting at or before it moves it; inserting just after it leaves it alone.
/// Removing part of a range shrinks it, and removing all of it marks it deleted while keeping the region it last
/// occupied. Tracking and untracking are not undoable.
/// </para>
/// <para>
/// A range on a sheet name the workbook does not have - because the sheet has not been added yet, or has been
/// removed - is detached: it keeps its region, does not move, and attaches to the sheet when one with that name is
/// added or renamed to it.
/// </para>
/// </summary>
public sealed class TrackedRangeCollection
{
    private readonly Workbook _workbook;

    private readonly Dictionary<string, TrackedRange> _detached = new(StringComparer.Ordinal);

    internal TrackedRangeCollection(Workbook workbook)
    {
        _workbook = workbook;
        _workbook.SheetAdded += (_, args) => Attach(args.Sheet);
        _workbook.SheetRenamed += (_, args) => Attach(args.Sheet);
        _workbook.SheetRemoved += (_, args) => Detach(args.Sheet);
    }

    /// <summary>
    /// Tracks <paramref name="region"/> on the sheet named <paramref name="sheetName"/> under <paramref name="key"/>,
    /// replacing whatever was tracked under that key on any sheet. If the workbook has no sheet with that name, the
    /// range is detached until it does.
    /// </summary>
    public TrackedRange Track(string key, string sheetName, IRegion region) =>
        Track(key, sheetName, region, isDeleted: false);

    internal TrackedRange Track(string key, string sheetName, IRegion region, bool isDeleted)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentException.ThrowIfNullOrEmpty(sheetName);

        Untrack(key);

        var sheet = _workbook.GetSheet(sheetName);
        if (sheet == null)
        {
            var detached = new TrackedRange(key, sheetName, region.Clone(), isDeleted);
            _detached[key] = detached;
            return Snapshot(detached);
        }

        sheet.TrackedRanges.Track(key, region, isDeleted);
        return sheet.TrackedRanges.Get(key)!;
    }

    /// <summary>
    /// Stops tracking <paramref name="key"/>. Returns false if nothing was tracked under it.
    /// </summary>
    public bool Untrack(string key)
    {
        var removed = _detached.Remove(key);
        foreach (var sheet in _workbook.Sheets)
            removed |= sheet.TrackedRanges.Untrack(key);

        return removed;
    }

    /// <summary>
    /// The range tracked under <paramref name="key"/>, or null if nothing is.
    /// </summary>
    public TrackedRange? Get(string key)
    {
        if (_detached.TryGetValue(key, out var detached))
            return Snapshot(detached);

        foreach (var sheet in _workbook.Sheets)
        {
            if (sheet.TrackedRanges.Get(key) is { } range)
                return range;
        }

        return null;
    }

    public bool Contains(string key) =>
        _detached.ContainsKey(key) || _workbook.Sheets.Any(sheet => sheet.TrackedRanges.Contains(key));

    /// <summary>
    /// Every tracked range, including deleted and detached ones.
    /// </summary>
    public IEnumerable<TrackedRange> GetAll() =>
        _workbook.Sheets.SelectMany(sheet => sheet.TrackedRanges.GetAll())
            .Concat(_detached.Values.Select(Snapshot)).ToList();

    private static TrackedRange Snapshot(TrackedRange range) =>
        range with { Region = range.Region.Clone() };

    private void Attach(Sheet sheet)
    {
        foreach (var range in _detached.Values.Where(r => r.SheetName == sheet.Name).ToList())
        {
            _detached.Remove(range.Key);
            sheet.TrackedRanges.Track(range.Key, range.Region, range.IsDeleted);
        }
    }

    private void Detach(Sheet sheet)
    {
        foreach (var range in sheet.TrackedRanges.GetAll())
        {
            sheet.TrackedRanges.Untrack(range.Key);
            _detached[range.Key] = range;
        }
    }
}
