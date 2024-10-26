namespace BlazorDatasheet.Core.Events;

public class ViewportScrollInfo
{
    public double SheetTop { get; set; }
    public double SheetLeft { get; set; }
    public double ContainerHeight { get; set; }
    public double ContainerWidth { get; set; }
    public double VisibleLeft { get; set; }
    public double VisibleTop { get; set; }
    public double ParentScrollTop { get; set; }
    public double ParentScrollLeft { get; set; }
}