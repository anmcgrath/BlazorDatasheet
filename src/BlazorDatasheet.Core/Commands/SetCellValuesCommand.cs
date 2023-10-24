using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class SetCellValuesCommand : IUndoableCommand
{
    private List<CellValueChange> _values;
    private List<CellValueChange> _undoValues;
    private int _minrow = int.MaxValue;
    private int _maxrow = int.MinValue;
    private int _minCol = int.MaxValue;
    private int _maxCol = int.MinValue;

    /// <summary>
    /// Command to set the values of a collection of cells to the values specified.
    /// </summary>
    /// <param name="values"></param>
    public SetCellValuesCommand(List<CellValueChange> values)
    {
        _values = values;
        _undoValues = new List<CellValueChange>();
    }

    public bool Execute(Sheet sheet)
    {
        // Get old values for undo
        _undoValues = new List<CellValueChange>();
        foreach (var valChange in _values)
        {
            var oldCellValue = sheet.GetValue(valChange.Row, valChange.Col);
            _undoValues.Add(new CellValueChange(valChange.Row, valChange.Col, oldCellValue));
            _minrow = Math.Min(_minrow, valChange.Row);
            _maxrow = Math.Max(_maxrow, valChange.Row);
            _minCol = Math.Min(_minCol, valChange.Col);
            _maxCol = Math.Max(_maxCol, valChange.Col);
        }

        var setValues = sheet.SetCellValuesImpl(_values);
        return setValues;
    }

    public bool Undo(Sheet sheet)
    {
        sheet.Selection.SetSingle(new Region(_minrow, _maxrow, _minCol, _maxCol));
        return sheet.SetCellValuesImpl(_undoValues);
    }
}