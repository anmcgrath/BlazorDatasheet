using BlazorDatasheet.Interfaces;
using BlazorDatasheet.Model;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Edit;

public class EditorManager : IEditorManager
{
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
    private Dictionary<string, Type> _editorTypes;
    public bool IsEditing => CurrentEditPosition != null;
    public bool IsSoftEdit { get; private set; }

    public EditorManager()
    {
        _editorTypes = new Dictionary<string, Type>();
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

    public void BeginEdit(int row, int col, Cell cell, bool isSoftEdit, EditEntryMode mode, string key,
        Action<Action> queueForNextRender)
    {
        if (IsEditing && CurrentEditPosition.Equals(row, col))
            return;

        this.CurrentEditPosition = new CellPosition { Row = row, Col = col };
        this.CurrentEditedCell = cell;
        this.IsSoftEdit = isSoftEdit;

        // Set the ActiveEditorType based on the cell's type - default to Text Editor
        if (this._editorTypes.ContainsKey(cell.Type))
            ActiveEditorType = _editorTypes[cell.Type];
        else
            ActiveEditorType = typeof(TextEditorComponent);

        // Because the ActiveEditor is null until the next re-render (unfortunately)
        // we need to queue the begin edit function until then
        queueForNextRender(() => { ActiveEditorComponent?.BeginEdit(mode, cell, key); });
    }

    public AcceptEditResult AcceptEdit()
    {
        if (!IsEditing || ActiveEditorComponent == null)
            return new AcceptEditResult(false, -1, -1, null, null, null);

        var activeCell = CurrentEditedCell;
        var currentRow = CurrentEditPosition.Row;
        var currentCol = CurrentEditPosition.Col;
        var editedValue = _editedValue;

        if (!ActiveEditorComponent.CanAcceptEdit())
            return new AcceptEditResult(false, currentRow, currentCol, activeCell,
                                        _initialValue, _initialValue);

        // Perform data validation
        var isValid = true;
        foreach (var validator in activeCell.Validators)
        {
            if (validator.IsValid(editedValue)) continue;
            if (validator.IsStrict)
                return AcceptEditResult.Reject(currentRow, currentCol);

            isValid = false;
        }

        activeCell.IsValid = isValid;

        // Try to set the cell's value to the new (edited) value
        var setCell = CurrentEditedCell?.SetValue(editedValue);
        if (setCell == true)
        {
            this.clearCurrentEdit();
            return new AcceptEditResult(true, currentRow, currentCol, activeCell,
                                        _initialValue,
                                        editedValue);
        }

        return new AcceptEditResult(false, currentRow, currentCol, activeCell, _initialValue,
                                    _initialValue);
    }

    public bool HandleKeyDown(string key, bool ctrlKey, bool shiftKey, bool altKey, bool metaKey)
    {
        if (!IsEditing)
            return false;

        if (ActiveEditorComponent == null)
            return false;

        return ActiveEditorComponent.HandleKey(key, ctrlKey, shiftKey, altKey, metaKey);
    }

    public CancelEditResult CancelEdit()
    {
        if (!ActiveEditorComponent.CanCancelEdit())
            return new CancelEditResult(false, "");

        this.clearCurrentEdit();

        return new CancelEditResult(true, "");
    }

    public void RegisterEditor<T>(string name) where T : ICellEditor
    {
        if (!_editorTypes.ContainsKey(name))
            _editorTypes.Add(name, typeof(T));
        _editorTypes[name] = typeof(T);
    }

    private void clearCurrentEdit()
    {
        this.CurrentEditPosition = null;
        this.CurrentEditedCell = null;
        this.ActiveEditorType = null;
        this._editedValue = null;
    }
}