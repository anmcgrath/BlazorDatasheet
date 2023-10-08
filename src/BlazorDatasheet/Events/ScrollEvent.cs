namespace BlazorDatasheet.Events;

public class ScrollEvent
{
    public double ScrollTop { get; set; }
    public double ScrollHeight { get; set; }
    public double ScrollWidth { get; set; }
    public double ScrollLeft { get; set; }
    public double ContainerHeight { get; set; }
    public double ContainerWidth { get; set; }
}