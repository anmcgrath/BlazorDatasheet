using System.Runtime.CompilerServices;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Services;

/// <summary>
/// Finds the datasheets that are showing a sheet. Components outside of a datasheet, such as a formula bar,
/// are given only the sheet, and use this to reach the datasheet for focus, scrolling and keyboard handling.
/// Nothing is kept alive by the registry.
/// </summary>
internal static class DatasheetRegistry
{
    private static readonly ConditionalWeakTable<Sheet, SheetViews> Views = new();

    public static SheetViews For(Sheet sheet) => Views.GetValue(sheet, _ => new SheetViews());

    private static readonly ConditionalWeakTable<Workbook, string> WorkbookIds = new();

    /// <summary>
    /// Identifies the workbook in the page, so that the views of its sheets can be told from the rest of the page.
    /// </summary>
    public static string WorkbookId(Workbook workbook) =>
        WorkbookIds.GetValue(workbook, _ => Guid.NewGuid().ToString("N"));
}

/// <summary>
/// The datasheets that are showing a particular sheet.
/// </summary>
internal sealed class SheetViews
{
    private readonly object _lock = new();

    // most recently registered or activated last
    private readonly List<WeakReference<Datasheet>> _datasheets = new();

    /// <summary>
    /// Fired when a datasheet starts or stops showing the sheet.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// The datasheets showing the sheet. The last is the one that was most recently active.
    /// </summary>
    public IReadOnlyList<Datasheet> Datasheets
    {
        get
        {
            lock (_lock)
            {
                var datasheets = new List<Datasheet>(_datasheets.Count);
                foreach (var reference in _datasheets)
                    if (reference.TryGetTarget(out var datasheet))
                        datasheets.Add(datasheet);
                return datasheets;
            }
        }
    }

    /// <summary>
    /// The datasheet that was most recently active, of those showing the sheet.
    /// </summary>
    public Datasheet? Active => Datasheets.LastOrDefault();

    public void Add(Datasheet datasheet)
    {
        lock (_lock)
        {
            RemoveCore(datasheet);
            _datasheets.Add(new WeakReference<Datasheet>(datasheet));
        }

        Changed?.Invoke();
    }

    public void Remove(Datasheet datasheet)
    {
        bool removed;
        lock (_lock)
            removed = RemoveCore(datasheet);

        if (removed)
            Changed?.Invoke();
    }

    /// <summary>
    /// Records that the datasheet is the one the user is working in.
    /// </summary>
    public void NoteActivated(Datasheet datasheet)
    {
        lock (_lock)
        {
            if (RemoveCore(datasheet))
                _datasheets.Add(new WeakReference<Datasheet>(datasheet));
        }
    }

    /// <summary>
    /// Removes the datasheet, and any that have been collected. Returns whether the datasheet was there.
    /// </summary>
    private bool RemoveCore(Datasheet datasheet)
    {
        var found = false;
        _datasheets.RemoveAll(x =>
        {
            if (!x.TryGetTarget(out var target))
                return true;
            if (!ReferenceEquals(target, datasheet))
                return false;
            found = true;
            return true;
        });
        return found;
    }
}
