using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Core.Patterns;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands.Data;

public class AutoFillCommand : IUndoableCommand
{
    private IRegion _fromRegion;
    private IRegion _toRegion;
    private CommandGroup _expandCommands = new();

    private ClearCellsCommand? _clearCellsCommand = null;

    public AutoFillCommand(IRegion fromRegion, IRegion toRegion)
    {
        _fromRegion = fromRegion;
        _toRegion = toRegion;
    }

    public bool Execute(Sheet sheet)
    {
        // Shrink/cut the content if the new region is smaller than the selection
        if (_fromRegion.Contains(_toRegion))
            ShrinkContent(sheet);
        else
            ExpandContent(sheet);

        sheet.Selection.Set(_toRegion);
        return true;
    }

    private void ExpandContent(Sheet sheet)
    {
        var fillDirection = GetFillDirection();
        // will always be only one region
        var fillRegion = _toRegion.Break(_fromRegion).First()!;
        var fillSize = GetOrthogonalSize(fillDirection, fillRegion);

        _expandCommands = new CommandGroup();
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

                        _expandCommands.AddCommand(
                            pattern.GetCommand(offset - pattern.Offsets.First(), repeatNo, cells[offset],
                                cellPosition));
                        repeatNo++;
                    }

                    appliedOffsets.Add(offset);
                }
            }
        }


        _expandCommands.Execute(sheet);
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

    private double GetSize(Direction direction, IRegion region)
    {
        if (direction == Direction.Down || direction == Direction.Up)
            return region.Height;
        if (direction == Direction.Left || direction == Direction.Right)
            return region.Width;

        return 0;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="direction"></param>
    /// <param name="region"></param>
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

    private void ShrinkContent(Sheet sheet)
    {
        _clearCellsCommand = new ClearCellsCommand(_fromRegion.Break(_toRegion));
        _clearCellsCommand.Execute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        if (_clearCellsCommand != null)
            _clearCellsCommand.Undo(sheet);

        _expandCommands?.Undo(sheet);

        return true;
    }
}