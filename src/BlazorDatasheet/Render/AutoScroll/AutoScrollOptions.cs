namespace BlazorDatasheet.Render.AutoScroll;

public class AutoScrollOptions
{
	/// <summary>
	/// The maximum number of pixels to scroll per <see cref="PollIntervalInMs"/>.
	/// The scroller converts this to a frame-independent speed.
	/// </summary>
	public double MaxVelocity { get; set; } = 200;

	/// <summary>
	/// The interval in milliseconds used to interpret <see cref="MaxVelocity"/>.
	/// Retained for compatibility; the scroller no longer polls.
	/// </summary>
	public int PollIntervalInMs { get; set; } = 200;

	/// <summary>
	/// How far inside the viewport edge, in pixels, a drag begins to scroll.
	/// </summary>
	public double EdgeThresholdPixels { get; set; } = 16;
}