namespace BlazorDatasheet.Render.AutoScroll;

public class AutoScrollOptions
{
    /// <summary>
    /// The max number of pixels to scroll at each poll interval.
    /// </summary>
    public double MaxVelocity { get; set; } = 100;

    /// <summary>
    /// The polling interval (in ms) that the autoscroller works at. Default is 200
    /// </summary>
    public int PollIntervalInMs { get; set; } = 200;
}