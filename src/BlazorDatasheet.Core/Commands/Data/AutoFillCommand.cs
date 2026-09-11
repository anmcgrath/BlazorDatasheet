using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Patterns;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

public class AutoFillCommand : BaseCommand, IUndoableCommand
{
    private IRegion _fromRegion;
    private IRegion _toRegion;

    /// <summary>
    /// The commands that were run by the last successful execution, kept so that we can undo them.
    /// </summary>
    private CommandGroup? _expandCommands;

    public AutoFillCommand(IRegion fromRegion, IRegion toRegion)
    {
        _fromRegion = fromRegion;
        _toRegion = toRegion;
    }

    /// <summary>
    /// Builds the commands that perform the fill, based on the current contents of the sheet.
    /// Side effect free - called both when checking protection and when executing.
    /// </summary>
    private CommandGroup BuildFillCommands(Sheet sheet)
    {
        var commands = new CommandGroup();
        var clearRegions = _fromRegion.Contains(_toRegion)
            ? _fromRegion.Break(_toRegion)
            : _toRegion.Break(_fromRegion);
        commands.AddCommand(new ClearCellsCommand(clearRegions));
        if (!_fromRegion.Contains(_toRegion))
            ExpandContent(sheet, commands);
        return commands;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        // built here (rather than up-front) so that the fill always samples the current cell contents,
        // e.g. when this command is part of a group that has already modified the source cells.
        var commands = BuildFillCommands(sheet);
        if (!sheet.Commands.ExecuteCommand(commands, useUndo: false))
            return false;

        _expandCommands = commands;
        sheet.Selection.Set(_toRegion);
        return true;
    }

    public override bool CanExecuteProtected(Sheet sheet) =>
        sheet.Protection.CanExecute(BuildFillCommands(sheet));

    private void ExpandContent(Sheet sheet, CommandGroup expandCommands)
    {
        var fillDirection = GetFillDirection();
        // will always be only one region
        var fillRegion = _toRegion.Break(_fromRegion).First()!;
        var fillSize = GetOrthogonalSize(fillDirection, fillRegion);

        for (int i = 0; i < fillSize; i++)
        {
            // figure out what patterns to apply
            var cells = GetCells(i, fillDirection, sheet);
            var lastCellPosition = new CellPosition(cells.Last().Row, cells.Last().Col);
            var patterns = GetPatterns(cells, sheet);
            var fillTotal = fillRegion.GetSize(fillDirection);

            var appliedOffsets = new HashSet<int>();
            foreach (var pattern in patterns)
            {
                foreach (var offset in pattern.Offsets)
                {
                    if (appliedOffsets.Contains(offset))
                        continue;

                    int repeatNo = 0;
                    while (offset + cells.Length * repeatNo < fillTotal)
                    {
                        var rowColOffsetFromEnd = offset + cells.Length * repeatNo;
                        var cellPosition =
                            GetCellPositionFromOffset(lastCellPosition, fillDirection, rowColOffsetFromEnd + 1);

                        expandCommands.AddCommand(
                            pattern.GetCommand(offset - pattern.Offsets.First(), repeatNo, cells[offset],
                                cellPosition));
                        repeatNo++;
                    }

                    appliedOffsets.Add(offset);
                }
            }
        }
    }

    private CellPosition GetCellPositionFromOffset(CellPosition cellPosition, Direction direction, int offset)
    {
        switch (direction)
        {
            case Direction.Down:
                return cellPosition with { row = cellPosition.row + offset };
            case Direction.Up:
                return cellPosition with { row = cellPosition.row - offset };
            case Direction.Right:
                return cellPosition with { col = cellPosition.col + offset };
            case Direction.Left:
                return cellPosition with { col = cellPosition.col - offset };
        }

        return cellPosition;
    }

    private IAutoFillPattern[] GetPatterns(IReadOnlyCell[] cells, Sheet sheet)
    {
        var patterns = new List<IAutoFillPattern>();
        var patternFinders = new[]
        {
            new NumberPatternFinder()
        };

        foreach (var patternFinder in patternFinders)
            patterns.AddRange(patternFinder.Find(cells));

        var matchedOffsets = patterns.SelectMany(x => x.Offsets)
            .ToHashSet();

        var offsets = Enumerable.Range(0, cells.Length)
            .Where(x => !matchedOffsets.Contains(x))
            .ToList();

        // Use the default auto fill pattern for offsets that aren't used
        patterns.Add(new DefaultAutofillPattern(sheet, offsets));

        return patterns.ToArray();
    }

    /// <summary>
    /// Returns the size of the region, in the direction orthogonal to the direction given.
    /// E.g if the direction is right/left, return the height of the region.
    /// This gives us how many rows/columns we then have to do a 1d fill on
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="region"></param>
    /// <returns></returns>
    private double GetOrthogonalSize(Direction direction, IRegion region)
    {
        if (direction == Direction.Down || direction == Direction.Up)
            return region.Width;
        if (direction == Direction.Left || direction == Direction.Right)
            return region.Height;

        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="direction"></param>
    /// <param name="sheet"></param>
    /// <returns>Returns only cell values, in order of the pattern that should be evaluated.</returns>
    private IReadOnlyCell[] GetCells(int offset, Direction direction, Sheet sheet)
    {
        switch (direction)
        {
            case Direction.Down:
                return sheet.Cells.GetCellsInRegion(new Region(_fromRegion.Top, _fromRegion.Bottom,
                    _fromRegion.Left + offset,
                    _fromRegion.Left + offset)).ToArray();
            case Direction.Up:
                return sheet.Cells.GetCellsInRegion(new Region(_fromRegion.Top, _fromRegion.Bottom,
                    _fromRegion.Left + offset,
                    _fromRegion.Left + offset)).Reverse().ToArray();
            case Direction.Right:
                return sheet.Cells.GetCellsInRegion(new Region(_fromRegion.Top + offset, _fromRegion.Top + offset,
                    _fromRegion.Left, _fromRegion.Right)).ToArray();
            case Direction.Left:
                return sheet.Cells.GetCellsInRegion(new Region(_fromRegion.Top + offset, _fromRegion.Top + offset,
                    _fromRegion.Left, _fromRegion.Right)).Reverse().ToArray();
        }

        return Array.Empty<IReadOnlyCell>();
    }

    private Direction GetFillDirection()
    {
        if (_toRegion.Height == _fromRegion.Height)
        {
            if (_toRegion.Right > _fromRegion.Right)
                return Direction.Right;
            else
                return Direction.Left;
        }

        if (_toRegion.Width == _fromRegion.Width)
        {
            if (_toRegion.Bottom > _fromRegion.Bottom)
                return Direction.Down;
            else
                return Direction.Up;
        }

        return Direction.None;
    }

    public bool Undo(Sheet sheet)
    {
        _expandCommands?.Undo(sheet);
        return true;
    }
}