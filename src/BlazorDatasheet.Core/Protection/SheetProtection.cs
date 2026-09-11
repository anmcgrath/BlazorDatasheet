using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Protection;

/// <summary>Sheet editing policy, independent of existing read-only settings.</summary>
public sealed class SheetProtection
{
    private readonly Sheet _sheet;
    private int _bypassDepth;
    internal int Revision { get; private set; }
    public bool IsProtected { get; private set; }
    public SheetProtectionOptions Options { get; private set; } = new();
    public event EventHandler? Changed;
    internal bool IsEnforced => IsProtected && _bypassDepth == 0;

    internal SheetProtection(Sheet sheet) => _sheet = sheet;

    public void Protect(SheetProtectionOptions? options = null)
    {
        var next = options ?? new SheetProtectionOptions();
        if (IsProtected && Options == next)
            return;
        IsProtected = true;
        Options = next;
        OnChanged();
    }

    public void Unprotect()
    {
        if (!IsProtected)
            return;
        IsProtected = false;
        OnChanged();
    }

    private void OnChanged()
    {
        Revision++;
        _sheet.Commands.ClearHistory();
        if (_sheet.Editor.EditCell is { } cell && !CanEdit(cell.Row, cell.Col))
            _sheet.Editor.CancelEdit();
        Changed?.Invoke(this, EventArgs.Empty);
        _sheet.MarkDirty(_sheet.Region);
    }

    /// <summary>
    /// Runs a synchronous trusted update. Do not pass async delegates. Existing read-only
    /// checks still apply. Trusted updates clear history so they cannot be replayed by users.
    /// </summary>
    public void RunUnprotected(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        RunUnprotected(() => { action(); return true; });
    }

    public T RunUnprotected<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var outermost = _bypassDepth++ == 0;
        try { return action(); }
        finally
        {
            _bypassDepth--;
            if (outermost && IsProtected)
                OnChanged();
        }
    }

    public bool IsLocked(int row, int col) =>
        _sheet.Cells.GetFormat(row, col)?.IsLocked ??
        _sheet.Columns.Formats.Get(col)?.IsLocked ??
        _sheet.Rows.Formats.Get(row)?.IsLocked ?? true;

    public bool CanEdit(int row, int col) => CanEdit(new Region(row, col));

    public bool CanEdit(IRegion region)
    {
        if (!IsEnforced)
            return true;
        return !ContainsLocked(region) && _sheet.Cells.GetMerges(region).All(x => !ContainsLocked(x));
    }

    /// <summary>Queries protection only; bounds, validation and legacy read-only checks remain separate.</summary>
    public bool Can(SheetOperation operation, IRegion? region = null)
    {
        if (!IsEnforced)
            return true;
        return operation switch
        {
            SheetOperation.EditCells => region != null && CanEdit(region),
            SheetOperation.FormatCells => Options.AllowFormatCells,
            SheetOperation.FormatRows => Options.AllowFormatRows,
            SheetOperation.FormatColumns => Options.AllowFormatColumns,
            SheetOperation.InsertRows => Options.AllowInsertRows,
            SheetOperation.InsertColumns => Options.AllowInsertColumns,
            SheetOperation.DeleteRows => Options.AllowDeleteRows && region != null && CanEdit(region),
            SheetOperation.DeleteColumns => Options.AllowDeleteColumns && region != null && CanEdit(region),
            SheetOperation.Sort => Options.AllowSort && region != null && CanEdit(region),
            SheetOperation.Filter => Options.AllowFilter,
            SheetOperation.Freeze => true,
            _ => false
        };
    }

    public bool CanFormat(IRegion region) => Can(region switch
    {
        RowRegion => SheetOperation.FormatRows,
        ColumnRegion => SheetOperation.FormatColumns,
        _ => SheetOperation.FormatCells
    }, region);

    /// <summary>Checks a command and all declared chained operations before mutation.</summary>
    public bool CanExecute(ICommand command)
    {
        if (!IsEnforced)
            return true;
        return command is IProtectedCommand declaration && declaration.CanExecuteProtected(_sheet) &&
               command.GetChainedBeforeCommands().All(CanExecute) &&
               command.GetChainedAfterCommands().All(CanExecute);
    }

    /// <summary>Resolves sparse format regions in precedence order, without visiting individual cells.</summary>
    public bool ContainsLocked(IRegion region)
    {
        var clipped = region.GetIntersection(_sheet.Region);
        if (clipped == null)
            return false;
        var unresolved = new List<IRegion> { clipped };
        var formats = _sheet.Cells.GetFormatData(clipped)
            .Select(x => (x.Region, x.Data.IsLocked))
            .Concat(_sheet.Columns.Formats.GetIntervals(clipped.Left, clipped.Right)
                .Select(x => ((IRegion)new ColumnRegion(x.Start, x.End), x.Data.IsLocked)))
            .Concat(_sheet.Rows.Formats.GetIntervals(clipped.Top, clipped.Bottom)
                .Select(x => ((IRegion)new RowRegion(x.Start, x.End), x.Data.IsLocked)));
        foreach (var (area, locked) in formats)
        {
            if (locked == null)
                continue;
            var remaining = new List<IRegion>();
            foreach (var part in unresolved)
            {
                if (part.GetIntersection(area) == null)
                    remaining.Add(part);
                else if (locked.Value)
                    return true;
                else
                    remaining.AddRange(part.Break(area));
            }
            unresolved = remaining;
            if (unresolved.Count == 0)
                return false;
        }
        return unresolved.Count != 0;
    }
}
