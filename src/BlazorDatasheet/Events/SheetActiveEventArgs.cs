namespace BlazorDatasheet.Events;

public class SheetActiveEventArgs
{
    public Datasheet DataSheet { get;  }
    public bool IsActive { get; }

    public SheetActiveEventArgs(Datasheet dataSheet, bool isActive)
    {
        DataSheet = dataSheet;
        IsActive = isActive;
    }
    
}