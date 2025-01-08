using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.Events.Edit;

public class BeforeAcceptEditEventArgs
{
    public IReadOnlyCell Cell { get; }
    public string EditString { get; }
    public CellValue EditCellValue { get; set; }

    /// <summary>
    /// Determines whether the edit is accepted or not.
    /// </summary>
    public bool AcceptEdit { get; set; } = true;

    /// <summary>
    /// Determines whether the edit is stopped from continuing.
    /// </summary>
    public bool StopEdit { get; private set; }

    public BeforeAcceptEditEventArgs(IReadOnlyCell cell, string editString, CellValue editCellValue)
    {
        Cell = cell;
        EditString = editString;
        EditCellValue = editCellValue;
    }

    /// <summary>
    /// Stop the edit, even if the edit is not accepted.
    /// </summary>
    public void StopEditing()
    {
        StopEdit = true;
    }
}