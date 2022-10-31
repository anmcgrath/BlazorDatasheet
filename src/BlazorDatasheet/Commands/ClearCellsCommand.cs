using BlazorDatasheet.Data;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Commands;

public class ClearCellsCommand : IUndoableCommand
{
    private readonly BRange _range;
    private readonly List<ValueChange> _clearCommandOccurences;

    public ClearCellsCommand(BRange range)
    {
        _clearCommandOccurences = new List<ValueChange>();
        _range = range.Clone();
    }

    public bool Execute(Sheet sheet)
    {
        foreach (var cell in _range.GetNonEmptyCells())
        {
            var oldValue = cell.GetValue();

            // When this is redone it'll update the new value to the old value.
            if (oldValue != null && !String.IsNullOrEmpty(oldValue.ToString()))
            {
                _clearCommandOccurences.Add(
                    new ValueChange(cell.Row, cell.Col, oldValue));
            }
        }

        // There were no empty cells in range so we can't clear anything
        if (!_clearCommandOccurences.Any())
            return false;

        sheet.ClearCelllsImpl(_range);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetCellValuesImpl(_clearCommandOccurences);
        return true;
    }
}