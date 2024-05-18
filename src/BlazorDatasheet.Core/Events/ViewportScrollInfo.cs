namespace BlazorDatasheet.Core.Events;

public class ViewportScrollInfo
{
    public double ScrollTop { get; set; }
    public double ScrollLeft { get; set; }
    public double ContainerHeight { get; set; }
    public double ContainerWidth { get; set; }
}