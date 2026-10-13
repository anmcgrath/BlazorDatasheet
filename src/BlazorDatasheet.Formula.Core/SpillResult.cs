using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Formula.Core;

/// <summary>
/// What happened when an array result was written from a formula cell.
/// </summary>
/// <param name="OldRegion">The region the formula spilled into before, or null.</param>
/// <param name="NewRegion">The region it spills into now, or null if it did not spill (blocked or scalar).</param>
/// <param name="Blocked">True when something was in the way and the formula cell holds #SPILL!.</param>
public readonly record struct SpillResult(IRegion? OldRegion, IRegion? NewRegion, bool Blocked);
