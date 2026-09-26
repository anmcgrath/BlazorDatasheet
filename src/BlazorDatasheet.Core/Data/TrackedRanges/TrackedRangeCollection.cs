using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// Ranges tracked under a key through row and column inserts and removals, and their undo and redo.
/// A tracked range is not a formula: it is never calculated and cannot be referenced by a formula.
/// <para>
/// Inserting inside a range grows it; inserting at or before it moves it; inserting just after it leaves it alone.
/// Removing part of a range shrinks it, and removing all of it - or its sheet - marks it deleted while keeping the
/// region it last occupied. Tracking and untracking are not undoable.
/// </para>
/// </summary>
public sealed class TrackedRangeCollection
{
    private readonly Workbook _workbook;

    // Ranges on sheets that have been removed from the workbook. They are kept as deleted ranges.
    private readonly Dictionary<string, TrackedRange> _orphaned = new(StringComparer.Ordinal);

    internal TrackedRangeCollection(Workbook workbook)
    {
        _workbook = workbook;
        _workbook.SheetRemoved += (_, args) => Orphan(args.Sheet);
    }

    /// <summary>
    /// Tracks <paramref name="region"/> on the sheet named <paramref name="sheetName"/> under <paramref name="key"/>,
    /// replacing whatever was tracked under that key on any sheet.
    /// </summary>
    /// <exception cref="ArgumentException">The workbook has no sheet named <paramref name="sheetName"/>.</exception>
    public TrackedRange Track(string key, string sheetName, IRegion region) =>
        Track(key, sheetName, region, isDeleted: false);

    internal TrackedRange Track(string key, string sheetName, IRegion region, bool isDeleted)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        var sheet = _workbook.GetSheet(sheetName);

        if (sheet == null && !isDeleted)
            throw new ArgumentException($"Sheet {sheetName} does not exist", nameof(sheetName));

        Untrack(key);

        if (sheet == null)
        {
            var orphan = new TrackedRange(key, sheetName, region.Clone(), IsDeleted: true);
            _orphaned[key] = orphan;
            return orphan;
        }

        sheet.TrackedRanges.Track(key, region, isDeleted);
        return sheet.TrackedRanges.Get(key)!;
    }

    /// <summary>
    /// Stops tracking <paramref name="key"/>. Returns false if nothing was tracked under it.
    /// </summary>
    public bool Untrack(string key)
    {
        var removed = _orphaned.Remove(key);
        foreach (var sheet in _workbook.Sheets)
            removed |= sheet.TrackedRanges.Untrack(key);

        return removed;
    }

    /// <summary>
    /// The range tracked under <paramref name="key"/>, or null if nothing is.
    /// </summary>
    public TrackedRange? Get(string key)
    {
        if (_orphaned.TryGetValue(key, out var orphan))
            return orphan;

        foreach (var sheet in _workbook.Sheets)
        {
            if (sheet.TrackedRanges.Get(key) is { } range)
                return range;
        }

        return null;
    }

    public bool Contains(string key) =>
        _orphaned.ContainsKey(key) || _workbook.Sheets.Any(sheet => sheet.TrackedRanges.Contains(key));

    /// <summary>
    /// Every tracked range, including deleted ones.
    /// </summary>
    public IEnumerable<TrackedRange> GetAll() =>
        _workbook.Sheets.SelectMany(sheet => sheet.TrackedRanges.GetAll()).Concat(_orphaned.Values).ToList();

    private void Orphan(Sheet sheet)
    {
        foreach (var range in sheet.TrackedRanges.GetAll())
        {
            sheet.TrackedRanges.Untrack(range.Key);
            _orphaned[range.Key] = range with { IsDeleted = true };
        }
    }
}
