using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Commands;

public class ClearCellsCommand : IUndoableCommand
{
    private readonly IEnumerable<IReadOnlyRange> _ranges;
    private readonly List<CellClearCommandOccurence> _clearCommandOccurences;

    public ClearCellsCommand(IEnumerable<IReadOnlyRange> ranges)
    {
        _ranges = ranges;
        _clearCommandOccurences = new List<CellClearCommandOccurence>();
    }

    public ClearCellsCommand(Range range) : this(new List<IReadOnlyRange>() { range })
    {
    }

    public bool Execute(Sheet sheet)
    {
        foreach (var range in _ranges)
        {
            foreach (var posn in range)
            {
                if (posn.InvalidPosition)
                    continue;
                var cell = sheet.GetCell(posn);
                var oldValue = cell.GetValue();

                _clearCommandOccurences.Add(new CellClearCommandOccurence(posn.Row, posn.Col, oldValue));
            }
        }

        sheet.ClearCells(_ranges);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        // TODO batch the cell resets so that cell changed events are only raised once
        foreach (var o in _clearCommandOccurences)
        {
            sheet.TrySetCellValue(o.Row, o.Col, o.OldValue);
        }

        return true;
    }
}

internal struct CellClearCommandOccurence
{
    public CellClearCommandOccurence(int row, int col, object oldValue)
    {
        Row = row;
        Col = col;
        OldValue = oldValue;
    }

    public int Row { get; }
    public int Col { get; }
    public object OldValue { get; }
}