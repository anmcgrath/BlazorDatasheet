namespace BlazorDatasheet.Services;

/// <summary>Browser focus ownership and its monotonically increasing transition version.</summary>
public class SheetFocusEventArgs
{
	public bool Focused { get; set; }
	public long Version { get; set; }
}