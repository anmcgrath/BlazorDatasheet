namespace BlazorDatasheet.Services;

/// <summary>Browser focus ownership, keyboard activation and their monotonically increasing transition version.</summary>
public class SheetFocusEventArgs
{
    public bool Focused { get; set; }

    /// <summary>Whether the sheet should handle keyboard input. Usually follows <see cref="Focused"/>.</summary>
    public bool Active { get; set; }

    /// <summary>
    /// True when the change came from the window or tab losing or regaining focus, rather than
    /// focus moving between elements on the page.
    /// </summary>
    public bool FromWindow { get; set; }

    public long Version { get; set; }
}
