namespace BlazorDatasheet.Model;

/// <summary>
/// Stores the current edit state
/// Required because if the control is virtualized we lose the state when it moves out of scope
/// </summary>
public class EditState
{
    public EditState(Func<bool>? onAcceptEdit, Func<bool>? onCancelEdit, object initialValue)
    {
        OnAcceptEdit = onAcceptEdit;
        OnCancelEdit = onCancelEdit;
        InitialValue = initialValue;
    }

    public readonly Func<bool>? OnAcceptEdit;
    public readonly Func<bool>? OnCancelEdit;
    public bool IsSoftEdit { get; set; }
    public object? InitialValue { get; }
    public object? NewValue { get; set; }
}