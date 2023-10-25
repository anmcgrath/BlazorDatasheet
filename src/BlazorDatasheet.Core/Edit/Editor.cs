using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Edit;
using BlazorDatasheet.Core.Interfaces;
using BlazorDatasheet.Formula.Core;

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

    public void BeginEdit(int row, int col, bool isSoftEdit = true, EditEntryMode mode = EditEntryMode.None,
        string? key = null)
    {
        if (this.IsEditing)
            return;

        var cell = Sheet.GetCell(row, col);
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
    /// Accepts the edit and sets the cell value with a set value command.
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
            parsedFormula = Sheet.FormulaEngine.ParseFormula(formulaString);
            if (!parsedFormula.IsValid())
            {
                Sheet.Dialog.Alert("Invalid formula");
                return false;
            }
        }

        object? formulaResult = isFormula ? Sheet.FormulaEngine.Evaluate(parsedFormula) : null;
        var editValue = isFormula ? formulaResult : this.EditValue;

        var beforeAcceptEdit = new BeforeAcceptEditEventArgs(EditCell, editValue, parsedFormula, formulaString);
        BeforeEditAccepted?.Invoke(this, beforeAcceptEdit);

        if (beforeAcceptEdit.AcceptEdit)
        {
            // run the validators that are strict. cancel edit if any fail
            var validationResult = Sheet.Validation.Validate(editValue, EditCell.Row, EditCell.Col);
            if (validationResult.IsStrictFail)
            {
                Sheet.Dialog.Alert(string.Join("\n", validationResult.FailMessages));
            }

            EditAccepted?.Invoke(
                this,
                new EditAcceptedEventArgs(EditCell.Row, EditCell.Col, beforeAcceptEdit.EditValue,
                                          beforeAcceptEdit.Formula, isFormula ? EditValue : null));

            if (isFormula && parsedFormula != null)
                Sheet.SetFormula(EditCell.Row, EditCell.Col, parsedFormula);
            else
                Sheet.SetCellValue(EditCell.Row, EditCell.Col, editValue);

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