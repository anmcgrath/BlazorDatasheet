using Microsoft.AspNetCore.Components.Web;

namespace BlazorDatasheet.Menu;

public class MenuShowOptions
{
    public string TargetId { get; set; }
    public string Placement { get; set; }
    public double Margin { get; set; }
    public string Trigger { get; }
    public double ClientX { get; }
    public double ClientY { get; }

    public MenuShowOptions(string targetId, string placement, double margin, string trigger, double clientX,
        double clientY)
    {
        TargetId = targetId;
        Placement = placement;
        Margin = margin;
        Trigger = trigger;
        ClientX = clientX;
        ClientY = clientY;
    }
}