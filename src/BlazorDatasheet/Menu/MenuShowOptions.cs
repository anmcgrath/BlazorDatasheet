using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Menu;

public class MenuShowOptions
{
    public string TargetId { get; set; }
    public string Placement { get; set; }
    public double Margin { get; set; }
    public string Trigger { get; }
    public MouseEventArgs Args { get; }

    public MenuShowOptions(string targetId, string placement, double margin, string trigger, MouseEventArgs args)
    {
        TargetId = targetId;
        Placement = placement;
        Margin = margin;
        Trigger = trigger;
        Args = args;
    }
}