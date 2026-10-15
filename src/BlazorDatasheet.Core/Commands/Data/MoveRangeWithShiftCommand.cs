using BlazorDatasheet.Core.Commands.RowCols;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Moves a block of cells and shifts the neighbouring cells to make room, as Excel's "Insert Cut Cells". The block
/// is taken out of its source and the cells after it shift left (<see cref="Axis.Col"/>) or up (<see cref="Axis.Row"/>)
/// to close the gap; it is inserted before the cell at the destination, which shifts right or down with the cells
/// after it. When the source spans every row (or column) of its sheet, whole columns (or rows) are moved, together
/// with their sizes, headings, visibility, groups, formats and filters.
/// <para>
/// The destination is given as it is before the move: moving B1 to D1 in a row a, b, c, d gives a, c, b, d, and
/// moving D1 to B1 gives a, d, b, c. References into the block follow it, and other references shift as they would
/// when cells are deleted and inserted.
/// </para>
/// <para>The command is executed on the target sheet; the source may be another sheet of the same workbook.</para>
/// </summary>
public class MoveRangeWithShiftCommand : BaseCommand, IUndoableCommand
{
    private readonly SheetRange _from;
    private readonly SheetRange _to;
    private readonly Axis _shiftAxis;
    private readonly CopyOptions? _options;

    private ICommand? _insert;
    private MoveRangeCommand? _move;
    private RowColInfoRestoreData? _rowColRestore;
    private ICommand? _remove;

    /// <summary>
    /// Where the moved block ends up, once the command has run.
    /// </summary>
    internal IRegion? FinalRegion { get; private set; }

    /// <param name="from">The cells to move. Only the single region of the range is used.</param>
    /// <param name="to">The cell the block is inserted before, as it is before the move. Only the top-left of the
    /// range is used.</param>
    /// <param name="shiftAxis"><see cref="Axis.Col"/> shifts cells horizontally, <see cref="Axis.Row"/> vertically.</param>
    /// <param name="options">What to move. By default everything is moved.</param>
    public MoveRangeWithShiftCommand(SheetRange from, SheetRange to, Axis shiftAxis, CopyOptions? options = null)
    {
        _from = from;
        _to = to;
        _shiftAxis = shiftAxis;
        _options = options?.Clone();
    }

    private readonly record struct Geometry(
        Sheet FromSheet,
        bool SameSheet,
        bool FullBand,
        int Count,
        IRegion Target,
        IRegion SourceAfterInsert,
        IRegion Final);

    private int Start(IRegion r) => _shiftAxis == Axis.Col ? r.Left : r.Top;
    private int End(IRegion r) => _shiftAxis == Axis.Col ? r.Right : r.Bottom;
    private int BandStart(IRegion r) => _shiftAxis == Axis.Col ? r.Top : r.Left;
    private int BandEnd(IRegion r) => _shiftAxis == Axis.Col ? r.Bottom : r.Right;

    private IRegion Shift(IRegion r, int amount) => _shiftAxis == Axis.Col
        ? new Region(r.Top, r.Bottom, r.Left + amount, r.Right + amount)
        : new Region(r.Top + amount, r.Bottom + amount, r.Left, r.Right);

    private IRegion FullBandRegion(int start, int count) => _shiftAxis == Axis.Col
        ? new ColumnRegion(start, start + count - 1)
        : new RowRegion(start, start + count - 1);

    /// <summary>
    /// The regions involved in the move, or null when it is invalid or does nothing.
    /// </summary>
    private Geometry? GetGeometry(Sheet target)
    {
        var fromSheet = _from.Sheet;
        var source = _from.Region.GetIntersection(fromSheet.Region);
        if (source == null)
            return null;

        var sameSheet = ReferenceEquals(fromSheet, target);
        var fullBand = _shiftAxis == Axis.Col
            ? source.Top == 0 && source.Bottom == fromSheet.NumRows - 1
            : source.Left == 0 && source.Right == fromSheet.NumCols - 1;
        var count = _shiftAxis == Axis.Col ? source.Width : source.Height;

        IRegion target0;
        bool sameBand;
        if (fullBand)
        {
            var toStart = Start(_to.Region);
            if (toStart < 0 || toStart > target.GetSize(_shiftAxis))
                return null;
            target0 = FullBandRegion(toStart, count);
            sameBand = sameSheet;
        }
        else
        {
            var top = _to.Region.Top;
            var left = _to.Region.Left;
            target0 = new Region(top, top + source.Height - 1, left, left + source.Width - 1);
            if (!target.Region.Contains(target0))
                return null;
            sameBand = sameSheet && BandStart(target0) == BandStart(source) && BandEnd(target0) == BandEnd(source);

            // A partial overlap of the bands would leave a gap that is not rectangular.
            var disjoint = BandEnd(target0) < BandStart(source) || BandStart(target0) > BandEnd(source);
            if (sameSheet && !sameBand && !disjoint)
                return null;
        }

        var ts = Start(target0);
        var ss = Start(source);
        var se = End(source);

        if (sameBand)
        {
            // Landing before the block or straight after it leaves everything where it is.
            if (ts == ss || ts == se + 1)
                return null;
            // The block can't be inserted inside itself.
            if (ts > ss && ts <= se)
                return null;
        }

        // Inserting before the block in its band pushes it along; removing it pulls back a target after it.
        var sourceAfterInsert = sameBand && ss > ts ? Shift(source, count) : source;
        var final = sameBand && ts > se ? Shift(target0, -count) : target0;

        if (fullBand)
        {
            sourceAfterInsert = FullBandRegion(Start(sourceAfterInsert), count);
            final = FullBandRegion(Start(final), count);
        }

        return new Geometry(fromSheet, sameSheet, fullBand, count, target0, sourceAfterInsert, final);
    }

    private BaseCommand CreateInsert(Geometry g) => g.FullBand
        ? new InsertRowsColsCommand(Start(g.Target), g.Count, _shiftAxis)
        : new InsertCellsCommand(g.Target, _shiftAxis);

    private MoveRangeCommand CreateMove(Geometry g, Sheet target) =>
        new(g.FromSheet.Range(g.SourceAfterInsert), target.Range(g.Target.Top, g.Target.Left), _options);

    private BaseCommand CreateRemove(Geometry g) => g.FullBand
        ? new RemoveRowColsCommand(Start(g.SourceAfterInsert), _shiftAxis, g.Count)
        : new RemoveCellsCommand(g.SourceAfterInsert, _shiftAxis);

    public override bool CanExecuteProtected(Sheet target)
    {
        if (GetGeometry(target) is not { } g)
            return true;

        var remove = CreateRemove(g);
        return CreateInsert(g).CanExecuteProtected(target) &&
               CreateMove(g, target).CanExecuteProtected(target) &&
               (g.SameSheet
                   ? remove.CanExecuteProtected(target)
                   : g.FromSheet.Protection.CanExecute(remove));
    }

    protected override bool CanExecuteCore(Sheet target)
    {
        // Moving and removing are checked as the command runs, since they depend on the cells after the insert.
        return GetGeometry(target) is { } g && CreateInsert(g).CanExecute(target);
    }

    protected override bool ExecuteCore(Sheet target)
    {
        _insert = null;
        _move = null;
        _rowColRestore = null;
        _remove = null;
        FinalRegion = null;

        if (GetGeometry(target) is not { } g)
            return false;

        var fromSheet = g.FromSheet;
        using var updates = target.SuspendUpdates();
        if (!g.SameSheet)
            fromSheet.BatchUpdates();

        try
        {
            var insert = CreateInsert(g);
            if (!insert.CanExecute(target) || !insert.Execute(target))
                return false;
            _insert = insert;

            var move = CreateMove(g, target);
            if (!move.CanExecute(target) || !move.Execute(target))
            {
                Rollback(target, fromSheet);
                return false;
            }

            _move = move;

            if (g.FullBand)
                _rowColRestore = target.GetRowColStore(_shiftAxis)
                    .CopyImpl(fromSheet.GetRowColStore(_shiftAxis), Start(g.SourceAfterInsert), g.Count,
                        Start(g.Target));

            var remove = CreateRemove(g);
            if (!remove.CanExecute(fromSheet) || !remove.Execute(fromSheet))
            {
                Rollback(target, fromSheet);
                return false;
            }

            _remove = remove;
            FinalRegion = g.Final;
            return true;
        }
        finally
        {
            if (!g.SameSheet)
                fromSheet.EndBatchUpdates();
        }
    }

    /// <summary>
    /// Undoes whichever steps have run, in reverse, and forgets them.
    /// </summary>
    private void Rollback(Sheet target, Sheet fromSheet)
    {
        UndoSteps(target, fromSheet);
        _insert = null;
        _move = null;
        _rowColRestore = null;
        _remove = null;
        FinalRegion = null;
    }

    private void UndoSteps(Sheet target, Sheet fromSheet)
    {
        if (_remove is IUndoableCommand remove)
            remove.Undo(fromSheet);
        if (_rowColRestore != null)
            target.GetRowColStore(_shiftAxis).Restore(_rowColRestore);
        _move?.Undo(target);
        if (_insert is IUndoableCommand insert)
            insert.Undo(target);
    }

    public bool Undo(Sheet target)
    {
        var fromSheet = _from.Sheet;
        var sameSheet = ReferenceEquals(fromSheet, target);

        using var updates = target.SuspendUpdates();
        if (!sameSheet)
            fromSheet.BatchUpdates();

        try
        {
            UndoSteps(target, fromSheet);
        }
        finally
        {
            if (!sameSheet)
                fromSheet.EndBatchUpdates();
        }

        return true;
    }

    public int RetainedCellCount =>
        new[] { _insert, _move, _remove }.OfType<IUndoableCommand>().Sum(x => x.RetainedCellCount);
}
