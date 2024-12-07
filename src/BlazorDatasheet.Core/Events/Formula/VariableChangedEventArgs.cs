using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Events.Formula;

public class VariableChangedEventArgs
{
    public string VariableName { get; }
    public CellValue? PreviousValue { get; }
    public CellValue? NewValue { get; }

    public VariableChangedEventArgs(string variableName, CellValue? previousValue, CellValue? newValue)
    {
        VariableName = variableName;
        PreviousValue = previousValue;
        NewValue = newValue;
    }
}