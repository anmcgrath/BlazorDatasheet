using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;
using CellFormula = BlazorDatasheet.Formula.Core.Interpreter.CellFormula;

namespace BlazorDatasheet.Core.Edit;

public class Editor
{
    public Sheet Sheet;

    /// <summary>
    /// Runs before the editor is accepted. Can set whether the edit is successful and whether the editor
    /// should be cleared.
    /// </summary>
    public event EventHandler<BeforeAcceptEditEventArgs>? BeforeEditAccepted;

    /// <summary>
    /// Runs before the editor is opened
    /// </summary>
    public event EventHandler<BeforeCellEditEventArgs>? BeforeCellEdit;

    /// <summary>
    /// Runs when an edit is accepted.
    /// </summary>
    public event EventHandler<EditAcceptedEventArgs>? EditAccepted;

    /// <summary>
    /// Runs when an edit is cancelled
    /// </summary>
    public event EventHandler<EditCancelledEventArgs>? EditCancelled;

    /// <summary>
    /// Runs when an edit has begun.
    /// </summary>
    public event EventHandler<EditBeginEventArgs>? EditBegin;

    /// <summary>
    /// Calls when the editor value has changed.
    /// </summary>
    public event EventHandler<string?>? EditValueChanged;

    /// <summary>
    /// Called when the edit has finished (canceled, accepted or closed by another means.)
    /// </summary>
    public event EventHandler<EditFinishedEventArgs> EditFinished;

    /// <summary>
    /// If the sheet is editing a value.
    /// </summary>
    public bool IsEditing { get; private set; }

    private string _editValue;

    /// <summary>
    /// The currently edited value
    /// </summary>
    public string EditValue
    {
        get => _editValue;
        set
        {
            _editValue = value;
            EditValueChanged?.Invoke(this, _editValue);
        }
    }

    public string EditorType { get; private set; }

    /// <summary>
    /// The currently edited cell.
    /// </summary>
    public IReadOnlyCell? EditCell { get; set; }

    public Editor(Sheet sheet)
    {
        Sheet = sheet;
    }

    /// <summary>
    /// Begins an edit at the row/column specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="isSoftEdit">If true, the editor should hold on to the edit when arrow keys are pressed.</param>
    /// <param name="mode">Details on the method that was used to begin the edit.</param>
    /// <param name="key">The key that was used to begin the edit, if the entry mode was using the keyboard</param>
    public void BeginEdit(int row, int col, bool isSoftEdit = true, EditEntryMode mode = EditEntryMode.None,
        string? key = null)
    {
        if (this.IsEditing)
            return;

        var cell = Sheet.Cells.GetCell(row, col);
        var beforeEditArgs = new BeforeCellEditEventArgs(cell, cell.GetValue<string>() ?? string.Empty, cell.Type);
        this.BeforeCellEdit?.Invoke(this, beforeEditArgs);

        if (beforeEditArgs.CancelEdit)
            return;

        EditValue = beforeEditArgs.EditValue;
        IsEditing = true;
        EditCell = cell;
        EditorType = beforeEditArgs.EditorType;

        EditBegin?.Invoke(
            this,
            new EditBeginEventArgs(cell,
                beforeEditArgs.EditValue,
                beforeEditArgs.EditorType,
                isSoftEdit,
                mode,
                key));
    }

    /// <summary>
    /// Cancels the edit without setting the cell value.
    /// </summary>
    /// <returns></returns>
    public bool CancelEdit()
    {
        if (this.IsEditing && EditCell != null)
        {
            EditCancelled?.Invoke(this, new EditCancelledEventArgs(EditCell.Row, EditCell.Col));
            EditFinished?.Invoke(this, new EditFinishedEventArgs(EditCell.Row, EditCell.Col));
            ClearEdit();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Accepts the edit and sets the cell value.
    /// </summary>
    public bool AcceptEdit()
    {
        if (!this.IsEditing || EditCell == null)
            return false;

        // Determine if it's a formula, and calculate.
        CellFormula? parsedFormula = null;
        var isFormula = Sheet.FormulaEngine.IsFormula(this.EditValue);
        var formulaString = isFormula ? this.EditValue : null;

        if (isFormula)
        {
            parsedFormula = Sheet.FormulaEngine.ParseFormula(formulaString!);
            if (!parsedFormula.IsValid())
            {
                Sheet?.Dialog?.Alert("Invalid formula");
                return false;
            }
        }

        var formulaResult = isFormula ? Sheet.FormulaEngine.Evaluate(parsedFormula) : CellValue.Empty;
        var editValue = isFormula ? formulaResult : new CellValue(this.EditValue);

        var beforeAcceptEdit = new BeforeAcceptEditEventArgs(EditCell, editValue, parsedFormula, formulaString);
        BeforeEditAccepted?.Invoke(this, beforeAcceptEdit);

        if (beforeAcceptEdit.AcceptEdit)
        {
            // run the validators that are strict. cancel edit if any fail
            var validationResult = Sheet.Validators.Validate(editValue, EditCell.Row, EditCell.Col);
            if (validationResult.IsStrictFail)
            {
                Sheet.Dialog.Alert(string.Join("\n", validationResult.FailMessages));
                return false;
            }

            EditAccepted?.Invoke(
                this,
                new EditAcceptedEventArgs(EditCell.Row, EditCell.Col, beforeAcceptEdit.EditValue,
                    beforeAcceptEdit.Formula, isFormula ? EditValue : null));


            if (isFormula && parsedFormula != null)
                Sheet.Cells.SetFormula(EditCell.Row, EditCell.Col, parsedFormula);
            else
                Sheet.Cells.SetValue(EditCell.Row, EditCell.Col, editValue);

            EditFinished?.Invoke(this, new EditFinishedEventArgs(EditCell.Row, EditCell.Col));
            this.ClearEdit();
            return true;
        }

        // Stop the edit even if the edit wasn't accepted
        if (beforeAcceptEdit.StopEdit)
        {
            EditFinished?.Invoke(this, new EditFinishedEventArgs(EditCell.Row, EditCell.Col));
            this.ClearEdit();
        }

        return false;
    }

    private void ClearEdit()
    {
        IsEditing = false;
        EditValue = string.Empty;
        EditorType = string.Empty;
        EditCell = null;
    }
}