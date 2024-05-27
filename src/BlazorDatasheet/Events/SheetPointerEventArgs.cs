namespace BlazorDatasheet.Events;

public class SheetPointerEventArgs
{
    public bool IsInsideSheet { get; set; }
    public int Row { get; set; }
    public int Col { get; set; }
    public double SheetX { get; set; }
    public double SheetY { get; set; }
    public bool AltKey { get; set; }
    public bool CtrlKey { get; set; }
    public bool ShiftKey { get; set; }
    public bool MetaKey { get; set; }
    public double PageX { get; set; }
    public double PageY { get; set; }
    public int MouseButton { get; set; }
}