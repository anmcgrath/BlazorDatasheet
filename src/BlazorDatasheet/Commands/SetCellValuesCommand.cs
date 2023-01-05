using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class SetCellValuesCommand : IUndoableCommand
{
    private IEnumerable<ValueChange> _values;
    private List<ValueChange> _undoValues;

    public SetCellValuesCommand(IEnumerable<ValueChange> values)
    {
        _values = values;
    }

    public bool Execute(Sheet sheet)
    {
        // Get old values for undo
        _undoValues = new List<ValueChange>();
        foreach (var valChange in _values)
        {
            var oldCellValue = sheet.GetCellValue(valChange.Row, valChange.Col);
            _undoValues.Add(new ValueChange(valChange.Row, valChange.Col, oldCellValue));
        }

        var setValues = sheet.SetCellValuesImpl(_values);
        return setValues;
    }

    public bool Undo(Sheet sheet)
    {
        return sheet.SetCellValuesImpl(_undoValues);
    }
}