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

    private static readonly ConditionalWeakTable<Workbook, WorkbookViews> WorkbookViewsTable = new();

    public static WorkbookViews For(Workbook workbook) =>
        WorkbookViewsTable.GetValue(workbook, x => new WorkbookViews(x));
}

/// <summary>
/// Which of the sheets of a workbook the user is working in, of those that are being shown.
/// </summary>
internal sealed class WorkbookViews
{
    private readonly WeakReference<Workbook> _workbook;
    private WeakReference<Sheet>? _lastActivated;
    private WeakReference<Sheet>? _lastCurrent;

    public WorkbookViews(Workbook workbook)
    {
        _workbook = new WeakReference<Workbook>(workbook);
        // fired when a formula edit begins or ends, as well as when its references change
        workbook.FormulaEditReferencesChanged += (_, _) => Refresh();
    }

    /// <summary>
    /// Fired when <see cref="CurrentSheet"/> changes.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// The sheet that the user is working in, which is the one that was last active. Focus leaving the
    /// workbook doesn't change it. Null if there hasn't been one, or it is no longer shown.
    /// </summary>
    public Sheet? CurrentSheet
    {
        get
        {
            if (!_workbook.TryGetTarget(out var workbook))
                return null;

            // picking references from another sheet doesn't take the user away from the formula
            if (workbook.ActiveFormulaEdit is { } formulaEdit)
                return formulaEdit.Sheet;

            if (_lastActivated?.TryGetTarget(out var sheet) != true)
                return null;

            return DatasheetRegistry.For(sheet!).Datasheets.Count > 0 ? sheet : null;
        }
    }

    /// <summary>
    /// Whether the selection of the sheet should be shown. Only the current sheet shows its selection,
    /// so that it is clear which sheet things outside of the datasheets, such as a formula bar, apply to.
    /// </summary>
    public bool ShowsSelection(Sheet sheet)
    {
        var current = CurrentSheet;
        return current == null || ReferenceEquals(current, sheet);
    }

    /// <summary>
    /// Records that the sheet is the one the user is working in.
    /// </summary>
    public void NoteActivated(Sheet sheet)
    {
        if (_lastActivated?.TryGetTarget(out var last) == true && ReferenceEquals(last, sheet))
            return;

        _lastActivated = new WeakReference<Sheet>(sheet);
        Refresh();
    }

    /// <summary>
    /// Whether the sheet is the one that was last recorded by <see cref="NoteActivated"/>.
    /// </summary>
    public bool WasLastActivated(Sheet sheet) =>
        _lastActivated?.TryGetTarget(out var last) == true && ReferenceEquals(last, sheet);

    /// <summary>
    /// Fires <see cref="Changed"/> if the current sheet is no longer what it was. Called when something
    /// that it depends on has changed.
    /// </summary>
    public void Refresh()
    {
        var current = CurrentSheet;
        Sheet? last = null;
        _lastCurrent?.TryGetTarget(out last);
        if (ReferenceEquals(current, last))
            return;

        _lastCurrent = current == null ? null : new WeakReference<Sheet>(current);
        Changed?.Invoke();
    }
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

    /// <summary>
    /// Returns whether the datasheet was showing the sheet.
    /// </summary>
    public bool Remove(Datasheet datasheet)
    {
        bool removed;
        lock (_lock)
            removed = RemoveCore(datasheet);

        if (removed)
            Changed?.Invoke();
        return removed;
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
