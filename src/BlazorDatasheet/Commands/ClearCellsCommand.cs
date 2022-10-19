using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;
using Range = BlazorDatasheet.Data.Range;

namespace BlazorDatasheet.Commands;

public class ClearCellsCommand : IUndoableCommand
{
    private readonly IEnumerable<IRange> _ranges;
    private readonly List<CellClearCommandOccurence> _clearCommandOccurences;

    public ClearCellsCommand(IEnumerable<IRange> ranges)
    {
        _ranges = ranges;
        _clearCommandOccurences = new List<CellClearCommandOccurence>();
    }

    public ClearCellsCommand(Range range) : this(new List<IRange>() { range })
    {
    }

    public bool Execute(Sheet sheet)
    {
        foreach (var range in _ranges)
        {
            var rangeInSheet = range
                .GetIntersection(sheet.Range);
            foreach (var cellPosition in rangeInSheet)
            {
                if (cellPosition.InvalidPosition)
                    continue;
                var cell = sheet.GetCell(cellPosition);
                var oldValue = cell.GetValue();

                _clearCommandOccurences.Add(
                    new CellClearCommandOccurence(cellPosition.Row, cellPosition.Col, oldValue));
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
            sheet.TrySetCellValueImpl(o.Row, o.Col, o.OldValue);
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