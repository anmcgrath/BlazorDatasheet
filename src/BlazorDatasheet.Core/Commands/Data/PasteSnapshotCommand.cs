using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Commands.Formatting;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Formula.Core.Interpreter;

namespace BlazorDatasheet.Core.Commands.Data;

/// <summary>
/// Writes a <see cref="ClipboardSnapshot"/> (content from outside the workbook) at one or more origins. Values,
/// formulas, formats, merges and types are written according to the <see cref="PasteMode"/>, as a single
/// undoable step. Blocks are clipped to the sheet.
/// </summary>
public class PasteSnapshotCommand : BaseCommand, IUndoableCommand
{
    private readonly ClipboardSnapshot _snapshot;
    private readonly IReadOnlyList<CellPosition> _origins;
    private readonly PasteMode _mode;
    private CommandGroup? _group;
    private Sheet? _builtFor;

    public PasteSnapshotCommand(ClipboardSnapshot snapshot, CellPosition origin, PasteMode mode = PasteMode.All)
        : this(snapshot, new[] { origin }, mode)
    {
    }

    public PasteSnapshotCommand(ClipboardSnapshot snapshot, IReadOnlyList<CellPosition> origins,
        PasteMode mode = PasteMode.All)
    {
        _snapshot = snapshot;
        _origins = origins;
        _mode = mode;
    }

    /// <summary>
    /// The regions the blocks land in, clipped to the sheet.
    /// </summary>
    public IEnumerable<IRegion> GetTargetRegions(Sheet sheet)
    {
        foreach (var origin in _origins)
        {
            var block = PasteTargetCalculator.GetBlockAt(_snapshot.Region, origin).GetIntersection(sheet.Region);
            if (block != null)
                yield return block;
        }
    }

    public override bool CanExecuteProtected(Sheet sheet) => Build(sheet).CanExecuteProtected(sheet);

    protected override bool CanExecuteCore(Sheet sheet) => Build(sheet).CanExecute(sheet);

    protected override bool ExecuteCore(Sheet sheet) => Build(sheet).Execute(sheet);

    public bool Undo(Sheet sheet) => _group?.Undo(sheet) ?? true;

    public int RetainedCellCount => _group?.RetainedCellCount ?? 0;

    private CommandGroup Build(Sheet sheet)
    {
        if (_group != null && ReferenceEquals(_builtFor, sheet))
            return _group;

        var commands = new List<ICommand>();
        foreach (var origin in _origins)
        {
            var block = PasteTargetCalculator.GetBlockAt(_snapshot.Region, origin).GetIntersection(sheet.Region);
            if (block == null)
                continue;

            if (_mode != PasteMode.Formats)
                AddValueCommands(sheet, origin, block, commands);
            if (_mode != PasteMode.Values)
                AddFormatCommands(sheet, origin, block, commands);
        }

        _group = new CommandGroup(commands);
        _builtFor = sheet;
        return _group;
    }

    private void AddValueCommands(Sheet sheet, CellPosition origin, IRegion block, List<ICommand> commands)
    {
        var formulas = new List<(int row, int col, CellFormula formula)>();
        var types = new List<(IRegion region, string type)>();
        var values = new CellValue[block.Height][];
        for (var r = 0; r < block.Height; r++)
        {
            values[r] = new CellValue[block.Width];
            for (var c = 0; c < block.Width; c++)
            {
                var row = block.Top + r;
                var col = block.Left + c;
                var cell = _snapshot[row - origin.row, col - origin.col];
                values[r][c] = CellValue.Empty;
                if (cell == null)
                    continue;

                if (_mode == PasteMode.All && cell.Type != null)
                    types.Add((new Region(row, col), cell.Type));

                if (_mode == PasteMode.All && cell.Formula != null)
                {
                    var parsed = sheet.FormulaEngine.ParseFormula(cell.Formula, sheet.Name);
                    if (parsed.IsValid())
                    {
                        if (_snapshot.Origin is { } source)
                            parsed.ShiftReferences(origin.row - source.row, origin.col - source.col, null);
                        formulas.Add((row, col, parsed));
                        continue;
                    }

                    values[r][c] = CellValue.Text(cell.Formula);
                    continue;
                }

                values[r][c] = cell.RawText != null
                    ? sheet.Cells.ConvertToCellValue(row, col, cell.RawText)
                    : cell.Value;
            }
        }

        // types first so the value conversion respects them when the paste is redone
        if (!sheet.Protection.IsEnforced)
            foreach (var (region, type) in types)
                commands.Add(new SetTypeCommand(region, type));

        commands.Add(new SetCellValuesCommand(block.Top, block.Left, values));
        foreach (var (row, col, formula) in formulas)
            commands.Add(new SetParsedFormulaCommand(row, col, formula));
    }

    private void AddFormatCommands(Sheet sheet, CellPosition origin, IRegion block, List<ICommand> commands)
    {
        for (var row = block.Top; row <= block.Bottom; row++)
        {
            var col = block.Left;
            while (col <= block.Right)
            {
                var format = _snapshot[row - origin.row, col - origin.col]?.Format;
                if (format == null)
                {
                    col++;
                    continue;
                }

                // runs of identical formats along the row become one command
                var end = col;
                while (end + 1 <= block.Right &&
                       format.Equals(_snapshot[row - origin.row, end + 1 - origin.col]?.Format))
                    end++;
                commands.Add(new SetFormatCommand(new Region(row, row, col, end), format.Clone()));
                col = end + 1;
            }
        }

        if (sheet.Protection.IsEnforced)
            return;

        foreach (var merge in _snapshot.Merges)
        {
            var shifted = merge.Clone();
            shifted.Shift(origin.row, origin.col);
            if (!block.Contains(shifted))
                continue;
            // a merge that would cut through an existing one is dropped rather than failing the paste
            if (!sheet.Cells.GetMerges(shifted).All(shifted.Contains))
                continue;
            // a formats-only paste leaves the values of the cells the merge hides alone
            commands.Add(new MergeCellsCommand(shifted, clearHiddenCells: _mode != PasteMode.Formats));
        }
    }
}
