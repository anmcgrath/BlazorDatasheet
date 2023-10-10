using BlazorDatasheet.Formula.Core;
using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Events.Edit;

public class BeforeAcceptEditEventArgs
{
    public IReadOnlyCell Cell { get; }
    public object? EditValue { get; }
    public CellFormula? Formula { get; }
    public object? EvaluatedFormulaValue { get; }

    /// <summary>
    /// Determines whether the edit is accepted or not.
    /// </summary>
    public bool AcceptEdit { get; set; } = true;

    /// <summary>
    /// Determines whether the edit is stopped from continuing.
    /// </summary>
    public bool StopEdit { get; private set; }

    public BeforeAcceptEditEventArgs(IReadOnlyCell cell, object? editValue, CellFormula? formula, object? evaluatedFormulaValue)
    {
        Cell = cell;
        EditValue = editValue;
        Formula = formula;
        EvaluatedFormulaValue = evaluatedFormulaValue;
    }

    /// <summary>
    /// Stop the edit, even if the edit is not accepted.
    /// </summary>
    public void StopEditing()
    {
        StopEdit = true;
    }
}