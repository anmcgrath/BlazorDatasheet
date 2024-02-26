using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.Events.Edit;

public class BeforeAcceptEditEventArgs
{
    public IReadOnlyCell Cell { get; }
    public CellValue EditValue { get; }
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

    public BeforeAcceptEditEventArgs(IReadOnlyCell cell, CellValue editValue, CellFormula? formula,
        string? formulaString)
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