using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Edit.Events;
using BlazorDatasheet.Interfaces;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Edit;

public class EditorManager : IEditorManager
{
    private Sheet _sheet;
    private readonly Action<Action> _queueForNextRender;

    /// <summary>
    /// Stores the actively edited value
    /// </summary>
    private object? _editedValue { get; set; }

    /// The Active Editor Component Instance - set by the edit renderer when editing starts
    internal ICellEditor ActiveEditorComponent;
    private object? _initialValue { get; set; }
    public CellPosition CurrentEditPosition { get; private set; }
    public IReadOnlyCell CurrentEditedCell { get; private set; }
    public bool IsEditing => !CurrentEditPosition.IsInvalid;
    public bool IsSoftEdit { get; private set; }

    public event EventHandler<AcceptEditEventArgs> EditAccepted;
    public event EventHandler<RejectEditEventArgs> EditRejected;
    public event EventHandler<CancelEditEventArgs> EditCancelled;
    public event EventHandler<BeginEditEventArgs> EditBegin;

    public event EventHandler<BeforeEditBeginEventArg> BeforeEditBegin;

    public EditorManager(Sheet sheet)
    {
        _sheet = sheet;
        // When called, runs the function next render cycle.
        CurrentEditPosition = new CellPosition(-1, -1);
    }

    public T GetEditedValue<T>()
    {
        try
        {
            return (T)_editedValue;
        }
        catch (Exception e)
        {
            return default(T);
        }
    }

    public void SetEditedValue<T>(T value) => _editedValue = value;

    /// <summary>
    /// Begin the editing process for the cell
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="cell"></param>
    /// <param name="isSoftEdit"></param>
    /// <param name="mode"></param>
    /// <param name="key"></param>
    public void BeginEdit(int row, int col, bool isSoftEdit, EditEntryMode mode, string key)
    {
        BeforeEditBegin?.Invoke(this, new BeforeEditBeginEventArg(row, col, isSoftEdit, mode, key));
        
        if (IsEditing && CurrentEditPosition.Equals(row, col))
            return;

        var cell = _sheet.GetCell(row, col);

        this.CurrentEditPosition = new CellPosition(row, col);
        this.CurrentEditedCell = cell;
        this.IsSoftEdit = isSoftEdit;

        EditBegin?.Invoke(this, new BeginEditEventArgs(row, col, isSoftEdit, mode, key));
    }

    /// <summary>
    /// Attempts to accept edit.
    /// Returns whether the edit was successful, e.g due to a data validation failure
    /// </summary>
    /// <returns></returns>
    public bool AcceptEdit()
    {
        if (!IsEditing || ActiveEditorComponent == null)
            return false;

        var currentRow = CurrentEditPosition.Row;
        var currentCol = CurrentEditPosition.Col;

        if (!ActiveEditorComponent.CanAcceptEdit())
        {
            this.emitRejectEdit(currentRow, currentCol, _initialValue, _editedValue);
            return false;
        }

        var setCell = _sheet.TrySetCellValue(currentRow, currentCol, _editedValue);

        if (setCell)
        {
            this.clearCurrentEdit();
            this.emitAcceptEdit(currentRow, currentCol, _initialValue, _editedValue);
            return true;
        }

        this.emitRejectEdit(currentRow, currentCol, _initialValue, _editedValue);
        return false;
    }

    private void emitAcceptEdit(int row, int col, object initialValue, object editedValue)
    {
        EditAccepted?.Invoke(this, new AcceptEditEventArgs(row, col, initialValue, editedValue));
    }

    private void emitRejectEdit(int row, int col, object initialValue, object editedValue)
    {
        EditRejected?.Invoke(this, new RejectEditEventArgs(row, col, initialValue, editedValue));
    }

    public bool HandleKeyDown(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey)
    {
        if (!IsEditing)
            return false;

        if (ActiveEditorComponent == null)
            return false;

        return ActiveEditorComponent.HandleKey(key, ctrlKey, shiftKey, altKey, metaKey);
    }

    public bool CancelEdit()
    {
        if (!ActiveEditorComponent.CanCancelEdit())
            return false;

        this.clearCurrentEdit();
        EditCancelled?.Invoke(this, new CancelEditEventArgs(""));

        return true;
    }

    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    private void clearCurrentEdit()
    {
        this.CurrentEditPosition = new CellPosition(-1, -1);
        this.CurrentEditedCell = null;
        this._editedValue = null;
    }
}