namespace BlazorDatasheet.Core.Data.Cells;

/// <summary>
/// What the spill store knows about one array formula. The anchor is the top-left of the stored region.
/// </summary>
/// <param name="IsBlocked">
/// True when the formula could not spill because something was in the way. The region is then where it
/// wanted to spill, kept so that clearing the obstruction recalculates the anchor, and the entry owns
/// no cells.
/// </param>
public readonly record struct SpillInfo(bool IsBlocked);
