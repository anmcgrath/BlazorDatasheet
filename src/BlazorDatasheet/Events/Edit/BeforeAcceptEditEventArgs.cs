using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Events.Edit;

public class BeforeAcceptEditEventArgs
{
    public IReadOnlyCell Cell { get; }
    public object? EditValue { get; }
    public CellFormula? Formula { get; }

    public string? FormulaString { get; }

    /// <summary>
    /// Determines whether the edit is accepted or not.
    /// </summary>
    public bool AcceptEdit { get; set; } = true;

    /// <summary>
    /// Determines whether the edit is stopped from continuing.
    /// </summary>
    public bool StopEdit { get; private set; }

    public BeforeAcceptEditEventArgs(IReadOnlyCell cell, object? editValue, CellFormula? formula, string? formulaString)
    {
        Cell = cell;
        EditValue = editValue;
        Formula = formula;
        FormulaString = formulaString;
    }

    /// <summary>
    /// Stop the edit, even if the edit is not accepted.
    /// </summary>
    public void StopEditing()
    {
        StopEdit = true;
    }
}