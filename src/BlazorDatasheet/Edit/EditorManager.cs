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
    private readonly CommandManager _commandManager;
    private readonly Action<Action> _queueForNextRender;

    /// <summary>
    /// Stores the actively edited value
    /// </summary>
    private object? _editedValue { get; set; }

    private object? _initialValue { get; set; }

    internal DynamicComponent? ActiveEditorContainer { get; set; }
    public CellPosition CurrentEditPosition { get; private set; }
    public Cell CurrentEditedCell { get; private set; }
    internal ICellEditor ActiveEditorComponent => (ICellEditor)ActiveEditorContainer?.Instance;
    internal Type? ActiveEditorType { get; private set; }
    public bool IsEditing => !CurrentEditPosition.InvalidPosition;
    public bool IsSoftEdit { get; private set; }

    public delegate void AcceptEditHandler(AcceptEditEventArgs e);

    public event AcceptEditHandler OnAcceptEdit;

    public delegate void RejectEditHandler(RejectEditEventArgs e);

    public event RejectEditHandler OnRejectEdit;

    public delegate void CancelEditHandler(CancelEditEventArgs e);

    public event CancelEditHandler OnCancelEdit;

    public EditorManager(Sheet sheet, CommandManager commandManager, Action<Action> queueForNextRender)
    {
        _sheet = sheet;
        _commandManager = commandManager;
        // When called, runs the function next render cycle.
        _queueForNextRender = queueForNextRender;
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

    public void BeginEdit(int row, int col, Cell cell, bool isSoftEdit, EditEntryMode mode, string key)
    {
        if (IsEditing && CurrentEditPosition.Equals(row, col))
            return;

        this.CurrentEditPosition = new CellPosition(row, col);
        this.CurrentEditedCell = cell;
        this.IsSoftEdit = isSoftEdit;

        // Set the ActiveEditorType based on the cell's type - default to Text Editor
        if (this._sheet.EditorTypes.ContainsKey(cell.Type))
            ActiveEditorType = _sheet.EditorTypes[cell.Type];
        else
            ActiveEditorType = typeof(TextEditorComponent);

        // Because the ActiveEditor is null until the next re-render (unfortunately)
        // we need to queue the begin edit function until then
        _queueForNextRender(() => { ActiveEditorComponent?.BeginEdit(mode, cell, key); });
    }

    public bool AcceptEdit()
    {
        if (!IsEditing || ActiveEditorComponent == null)
            return false;

        var currentRow = CurrentEditPosition.Row;
        var currentCol = CurrentEditPosition.Col;
        var editedValue = _editedValue;

        if (!ActiveEditorComponent.CanAcceptEdit())
        {
            this.onRejectEdit(currentRow, currentCol, _initialValue, editedValue);
            return false;
        }

        var setCell = _commandManager.ExecuteCommand(new ChangeCellValueCommand(currentRow, currentCol, editedValue));

        if (setCell)
        {
            this.clearCurrentEdit();
            this.onAcceptEdit(currentRow, currentCol, _initialValue, editedValue);
            return true;
        }

        this.onRejectEdit(currentRow, currentCol, _initialValue, editedValue);
        return false;
    }

    private void onAcceptEdit(int row, int col, object initialValue, object editedValue)
    {
        OnAcceptEdit?.Invoke(new AcceptEditEventArgs(row, col, initialValue, editedValue));
    }

    private void onRejectEdit(int row, int col, object initialValue, object editedValue)
    {
        OnRejectEdit?.Invoke(new RejectEditEventArgs(row, col, initialValue, editedValue));
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
        OnCancelEdit?.Invoke(new CancelEditEventArgs(""));

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
        this.ActiveEditorType = null;
        this._editedValue = null;
    }
}