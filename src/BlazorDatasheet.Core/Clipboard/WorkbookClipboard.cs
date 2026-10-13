using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Data;
using BlazorDatasheet.Core.Events.Layout;
using BlazorDatasheet.Core.Events.Visual;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Clipboard;

/// <summary>
/// The workbook's own clipboard: the cells last copied or cut from any of its sheets, pasted with values,
/// formulas (references adjusted), formats, merges and types intact. The entry refers to the live source
/// cells and is dropped when they change, as Excel ends copy mode when the source is edited.
/// </summary>
public sealed class WorkbookClipboard
{
    private readonly Workbook _workbook;
    private ClipboardEntry? _current;

    /// <summary>
    /// The entry on the clipboard, or null when nothing has been copied or cut.
    /// </summary>
    public ClipboardEntry? Current => _current;

    public bool IsActive => _current != null;

    /// <summary>
    /// Raised when the entry is set or cleared.
    /// </summary>
    public event EventHandler<ClipboardChangedEventArgs>? Changed;

    /// <summary>
    /// Raised before any paste into a sheet of the workbook, internal or external. Cancellable.
    /// </summary>
    public event EventHandler<BeforePasteEventArgs>? BeforePaste;

    /// <summary>
    /// Raised after a paste has been written.
    /// </summary>
    public event EventHandler<PastedEventArgs>? Pasted;

    internal WorkbookClipboard(Workbook workbook)
    {
        _workbook = workbook;
        _workbook.SheetRemoved += OnSheetRemoved;
    }

    /// <summary>
    /// Puts the cells of <paramref name="range"/> on the clipboard to be copied.
    /// </summary>
    public ClipboardEntry Copy(SheetRange range) => Set(range, ClipboardMode.Copy);

    /// <summary>
    /// Puts the cells of <paramref name="range"/> on the clipboard to be moved. The source is cleared when
    /// the entry is pasted.
    /// </summary>
    public ClipboardEntry Cut(SheetRange range) => Set(range, ClipboardMode.Cut);

    /// <summary>
    /// Ends copy mode.
    /// </summary>
    public void Clear() => Replace(null);

    private ClipboardEntry Set(SheetRange range, ClipboardMode mode)
    {
        var region = range.Region.GetIntersection(range.Sheet.Region);
        if (region == null)
            throw new ArgumentException("The range lies outside its sheet", nameof(range));

        var entry = new ClipboardEntry(Guid.NewGuid(), new SheetRange(range.Sheet, region), mode);
        Replace(entry);
        return entry;
    }

    private void Replace(ClipboardEntry? entry)
    {
        if (_current == null && entry == null)
            return;

        var old = _current;
        if (old != null)
            Unwatch(old.Sheet);
        _current = entry;
        if (entry != null)
            Watch(entry.Sheet);
        Changed?.Invoke(this, new ClipboardChangedEventArgs(old, entry));
    }

    /// <summary>
    /// Raises <see cref="BeforePaste"/> for a paste of external content. Returns false if cancelled.
    /// </summary>
    internal bool RaiseBeforePaste(BeforePasteEventArgs args)
    {
        BeforePaste?.Invoke(this, args);
        return !args.Cancel;
    }

    internal void RaisePasted(PastedEventArgs args) => Pasted?.Invoke(this, args);

    /// <summary>
    /// Whether <see cref="Paste"/> would write anything.
    /// </summary>
    public bool CanPaste(Sheet target, IRegion selection, PasteMode mode = PasteMode.All)
    {
        var plan = Plan(target, selection, mode);
        return plan != null && plan.Value.command.CanExecute(target) &&
               (plan.Value.mode == PasteMode.Formats || !target.Cells.ContainsReadOnly(plan.Value.region));
    }

    /// <summary>
    /// Pastes the entry into <paramref name="target"/> at <paramref name="selection"/> and returns the region
    /// written, or null if nothing was pasted. A copied block tiles across a selection that is an exact
    /// multiple of its size. A cut block is moved, and the clipboard is cleared afterwards.
    /// </summary>
    public IRegion? Paste(Sheet target, IRegion selection, PasteMode mode = PasteMode.All)
    {
        var entry = _current;
        if (entry == null)
            return null;

        var selected = selection.GetIntersection(target.Region);
        if (selected == null)
            return null;

        var args = new BeforePasteEventArgs(target, selected, mode, entry);
        if (!RaiseBeforePaste(args))
            return null;

        var plan = Plan(target, selected, args.Mode);
        if (plan == null)
            return null;

        var (command, region, effectiveMode) = plan.Value;
        if (effectiveMode != PasteMode.Formats && target.Cells.ContainsReadOnly(region))
            return null;

        if (!target.Commands.ExecuteCommand(command))
            return null;

        var wasCut = entry.Mode == ClipboardMode.Cut;
        if (wasCut)
            Clear();

        Pasted?.Invoke(this, new PastedEventArgs(target, region, effectiveMode, wasCut));
        return region;
    }

    private (ICommand command, IRegion region, PasteMode mode)? Plan(Sheet target, IRegion selection, PasteMode mode)
    {
        var entry = _current;
        if (entry == null)
            return null;

        var selected = selection.GetIntersection(target.Region);
        if (selected == null)
            return null;

        var source = entry.Source.Region;
        if (entry.Mode == ClipboardMode.Cut)
        {
            // Excel only offers a plain paste after a cut
            var block = PasteTargetCalculator.GetBlockAt(source, selected.TopLeft).GetIntersection(target.Region);
            if (block == null)
                return null;
            var move = new MoveRangeCommand(entry.Source, target.Range(selected.TopLeft.row, selected.TopLeft.col));
            return (move, block, PasteMode.All);
        }

        var options = mode switch
        {
            PasteMode.Values => CopyOptions.ValuesOnly,
            PasteMode.Formats => CopyOptions.FormatsOnly,
            _ => new CopyOptions()
        };

        var origins = PasteTargetCalculator.GetTileOrigins(source, selected, allowTiling: true);
        IRegion written = PasteTargetCalculator.GetBlockAt(source, origins[0]);
        foreach (var origin in origins.Skip(1))
            written = written.GetBoundingRegion(PasteTargetCalculator.GetBlockAt(source, origin));
        written = written.GetIntersection(target.Region);
        if (written == null)
            return null;

        var targets = origins.Select(o => target.Range(o.row, o.col)).ToArray();
        return (new CopyRangeCommand(entry.Source, targets, options), written, mode);
    }

    #region invalidation

    private void Watch(Sheet sheet)
    {
        sheet.Cells.CellsChanged += OnCellsChanged;
        sheet.Cells.FormatChanged += OnFormatChanged;
        sheet.Cells.RegionMerged += OnRegionMergedOrUnMerged;
        sheet.Cells.RegionUnMerged += OnRegionMergedOrUnMerged;
        sheet.Rows.Inserted += OnRowColInsertedOrRemoved;
        sheet.Rows.Removed += OnRowColInsertedOrRemoved;
        sheet.Columns.Inserted += OnRowColInsertedOrRemoved;
        sheet.Columns.Removed += OnRowColInsertedOrRemoved;
    }

    private void Unwatch(Sheet sheet)
    {
        sheet.Cells.CellsChanged -= OnCellsChanged;
        sheet.Cells.FormatChanged -= OnFormatChanged;
        sheet.Cells.RegionMerged -= OnRegionMergedOrUnMerged;
        sheet.Cells.RegionUnMerged -= OnRegionMergedOrUnMerged;
        sheet.Rows.Inserted -= OnRowColInsertedOrRemoved;
        sheet.Rows.Removed -= OnRowColInsertedOrRemoved;
        sheet.Columns.Inserted -= OnRowColInsertedOrRemoved;
        sheet.Columns.Removed -= OnRowColInsertedOrRemoved;
    }

    private void OnCellsChanged(object? sender, CellDataChangedEventArgs e)
    {
        var source = _current?.Source.Region;
        if (source == null)
            return;
        if (e.Positions.Any(source.Contains) || e.Regions.Any(source.Intersects))
            Clear();
    }

    private void OnFormatChanged(object? sender, FormatChangedEventArgs e)
    {
        if (_current?.Source.Region.Intersects(e.Region) == true)
            Clear();
    }

    private void OnRegionMergedOrUnMerged(object? sender, IRegion region)
    {
        if (_current?.Source.Region.Intersects(region) == true)
            Clear();
    }

    private void OnRowColInsertedOrRemoved(object? sender, object e) => Clear();

    private void OnSheetRemoved(object? sender, WorkbookSheetRemovedEventArgs e)
    {
        if (_current != null && ReferenceEquals(_current.Sheet, e.Sheet))
            Clear();
    }

    #endregion
}
