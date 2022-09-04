namespace BlazorDatasheet.Edit;

public class CancelEditResult
{
    public readonly bool Cancelled;
    public readonly string Message;

    public CancelEditResult(bool cancelled, string message)
    {
        Cancelled = cancelled;
        Message = message;
    }
}