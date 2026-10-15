using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Copies a block of cells into the target sheet, first shifting the cells at the destination right
/// (<see cref="Axis.Col"/>) or down (<see cref="Axis.Row"/>) to make room, as Excel's "Insert Copied Cells".
/// The source may be on another sheet of the same workbook. When it lies after the destination in the shifted band,
/// it is copied from where the shift leaves it; a source split by the shift is refused.
/// </summary>
internal class InsertCopiedCellsCommand : BaseCommand, IUndoableCommand
{
    private readonly SheetRange _from;
    private readonly CellPosition _to;
    private readonly Axis _shiftAxis;
    private readonly CopyOptions _options;

    private InsertCellsCommand? _insert;
    private CopyRangeCommand? _copy;

    /// <summary>
    /// Whether the last execution shifted the source cells along with the cells after the destination.
    /// </summary>
    internal bool SourceShifted { get; private set; }

    /// <param name="from">The cells to copy. Only the single region of the range is used.</param>
    /// <param name="to">The top-left of the block the cells are inserted at.</param>
    /// <param name="shiftAxis"><see cref="Axis.Col"/> shifts cells right, <see cref="Axis.Row"/> shifts cells down.</param>
    /// <param name="options">What to copy. By default everything is copied.</param>
    public InsertCopiedCellsCommand(SheetRange from, CellPosition to, Axis shiftAxis, CopyOptions? options = null)
    {
        _from = from;
        _to = to;
        _shiftAxis = shiftAxis;
        _options = options?.Clone() ?? new CopyOptions();
    }

    /// <summary>
    /// The block written at the destination, or null when the paste is invalid.
    /// </summary>
    internal IRegion? GetTargetRegion(Sheet target) => GetGeometry(target)?.Target;

    private int Start(IRegion r) => _shiftAxis == Axis.Col ? r.Left : r.Top;
    private int End(IRegion r) => _shiftAxis == Axis.Col ? r.Right : r.Bottom;
    private int BandStart(IRegion r) => _shiftAxis == Axis.Col ? r.Top : r.Left;
    private int BandEnd(IRegion r) => _shiftAxis == Axis.Col ? r.Bottom : r.Right;

    private IRegion Shift(IRegion r, int amount) => _shiftAxis == Axis.Col
        ? new Region(r.Top, r.Bottom, r.Left + amount, r.Right + amount)
        : new Region(r.Top + amount, r.Bottom + amount, r.Left, r.Right);

    private (IRegion Target, IRegion SourceAfterInsert, bool Shifted)? GetGeometry(Sheet target)
    {
        var source = _from.Region.GetIntersection(_from.Sheet.Region);
        if (source == null)
            return null;

        IRegion block = new Region(_to.row, _to.row + source.Height - 1, _to.col, _to.col + source.Width - 1);
        if (!target.Region.Contains(block))
            return null;

        if (!ReferenceEquals(_from.Sheet, target))
            return (block, source, false);

        var bandsDisjoint = BandEnd(source) < BandStart(block) || BandStart(source) > BandEnd(block);
        if (bandsDisjoint || End(source) < Start(block))
            return (block, source, false);

        // Wholly in the shifted half-band: the source moves along with the cells after the destination.
        var withinBand = BandStart(source) >= BandStart(block) && BandEnd(source) <= BandEnd(block);
        if (withinBand && Start(source) >= Start(block))
            return (block, Shift(source, _shiftAxis == Axis.Col ? source.Width : source.Height), true);

        return null;
    }

    private CopyRangeCommand CreateCopy((IRegion Target, IRegion SourceAfterInsert, bool Shifted) g, Sheet target) =>
        new(_from.Sheet.Range(g.SourceAfterInsert), target.Range(g.Target.Top, g.Target.Left), _options);

    public override bool CanExecuteProtected(Sheet target)
    {
        if (GetGeometry(target) is not { } g)
            return true;
        return new InsertCellsCommand(g.Target, _shiftAxis).CanExecuteProtected(target) &&
               CreateCopy(g, target).CanExecuteProtected(target);
    }

    protected override bool CanExecuteCore(Sheet target)
    {
        // The copy is checked as the command runs, since it depends on the cells after the insert.
        return GetGeometry(target) is { } g && new InsertCellsCommand(g.Target, _shiftAxis).CanExecute(target);
    }

    protected override bool ExecuteCore(Sheet target)
    {
        _insert = null;
        _copy = null;
        SourceShifted = false;

        if (GetGeometry(target) is not { } g)
            return false;

        using var updates = target.SuspendUpdates();

        var insert = new InsertCellsCommand(g.Target, _shiftAxis);
        if (!insert.CanExecute(target) || !insert.Execute(target))
            return false;
        _insert = insert;

        var copy = CreateCopy(g, target);
        if (!copy.CanExecute(target) || !copy.Execute(target))
        {
            _insert.Undo(target);
            _insert = null;
            return false;
        }

        _copy = copy;
        SourceShifted = g.Shifted;
        return true;
    }

    public bool Undo(Sheet target)
    {
        using var updates = target.SuspendUpdates();
        _copy?.Undo(target);
        _insert?.Undo(target);
        return true;
    }

    public int RetainedCellCount => (_insert?.RetainedCellCount ?? 0) + (_copy?.RetainedCellCount ?? 0);
}
