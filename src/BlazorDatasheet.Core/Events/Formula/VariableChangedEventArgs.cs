using BlazorDatasheet.Formula.Core;

namespace BlazorDatasheet.Core.Events.Formula;

public enum VariableChangeKind
{
    Added,
    Updated,
    Removed,
    Recalculated
}

/// <summary>
/// Provides the state of a named variable before and after it changed.
/// </summary>
public class VariableChangedEventArgs : EventArgs
{
    public string Name { get; }
    public VariableChangeKind ChangeKind { get; }
    public CellValue? OldValue { get; }
    public CellValue? NewValue { get; }
    public string? OldFormula { get; }
    public string? NewFormula { get; }

    public VariableChangedEventArgs(string name, VariableChangeKind changeKind, CellValue? oldValue,
        CellValue? newValue, string? oldFormula, string? newFormula)
    {
        Name = name;
        ChangeKind = changeKind;
        OldValue = oldValue;
        NewValue = newValue;
        OldFormula = oldFormula;
        NewFormula = newFormula;
    }
}
