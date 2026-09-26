using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Data;

/// <summary>
/// A snapshot of a range tracked under a key.
/// </summary>
/// <param name="Key">The key the range is tracked under, unique within the workbook.</param>
/// <param name="SheetName">The name of the sheet the range is on, as it is now.</param>
/// <param name="Region">The range's region now, or the region it last occupied if it has been deleted.</param>
/// <param name="IsDeleted">True if every row or column of the range has been removed, or its sheet has.</param>
public sealed record TrackedRange(string Key, string SheetName, IRegion Region, bool IsDeleted);
