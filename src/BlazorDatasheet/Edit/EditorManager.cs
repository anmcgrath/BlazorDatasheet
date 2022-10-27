using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using BlazorDatasheet.Edit.DefaultComponents;
using BlazorDatasheet.Edit.Events;
using BlazorDatasheet.Interfaces;
using Microsoft.AspNetCore.Components;

namespace BlazorDatasheet.Edit;

public class EditorManager
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





    private void emitAcceptEdit(int row, int col, object initialValue, object editedValue)
    {
        EditAccepted?.Invoke(this, new AcceptEditEventArgs(row, col, initialValue, editedValue));
    }

    private void emitRejectEdit(int row, int col, object initialValue, object editedValue)
    {
        EditRejected?.Invoke(this, new RejectEditEventArgs(row, col, initialValue, editedValue));
    }





    public void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }


}