namespace BlazorDatasheet.Edit.Events;

public class CancelEditEventArgs
{
    public readonly string Message;

    public CancelEditEventArgs(string message)
    {
        Message = message;
    }
}