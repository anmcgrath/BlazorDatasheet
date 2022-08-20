namespace BlazorDatasheet.Model;

/// <summary>
/// Stores the current edit state
/// Required because if the control is virtualised we lose info when it moves out of scope
/// </summary>
public class EditState
{
    public EditState(Func<bool>? onAcceptEdit, Func<bool>? onCancelEdit, Cell cell)
    {
        OnAcceptEdit = onAcceptEdit;
        OnCancelEdit = onCancelEdit;
        Cell = cell;
    }

    public readonly Func<bool>? OnAcceptEdit;
    public readonly Func<bool>? OnCancelEdit;
    public bool IsSoftEdit { get; set; }
    public string? EditString { get; set; }
    public readonly Cell Cell;
}