namespace BlazorDatasheet.Model;

/// <summary>
/// Stores the current edit state
/// Required because if the control is virtualised we lose info when it moves out of scope
/// </summary>
public class EditState
{
    public Func<bool>? OnAcceptEdit { get; set; }
    public Func<bool>? OnCancelEdit { get; set; }
    public bool CanCancelEdit { get; set; }
    public bool CanAcceptEdit { get; set; }
    public bool IsSoftEdit { get; set; }
    public string? EditString { get; set; }
    public Cell Cell { get; set; }
}