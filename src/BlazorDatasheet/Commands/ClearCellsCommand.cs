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
        _range = range;
    }

    public bool Execute(Sheet sheet)
    {
        foreach (var cell in _range.GetCells())
        {
            var oldValue = cell.GetValue();

            // When this is redone it'll update the new value to the old value.
            if (oldValue != null && !String.IsNullOrEmpty(oldValue.ToString()))
            {
                _clearCommandOccurences.Add(
                    new ValueChange(cell.Row, cell.Col, oldValue));
            }
        }

        sheet.ClearCelllsImpl(_range);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.SetCellValuesImpl(_clearCommandOccurences);
        return true;
    }
}